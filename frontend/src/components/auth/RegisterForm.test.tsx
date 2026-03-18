import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { render, createMockAuthResponse } from '../../test/test-utils';
import { RegisterForm } from './RegisterForm';
import { useAuthStore } from '../../stores/authStore';

describe('RegisterForm', () => {
  const user = userEvent.setup();

  beforeEach(() => {
    useAuthStore.setState({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,
    });
    vi.clearAllMocks();
  });

  it('should render all form fields', () => {
    render(<RegisterForm />);

    expect(screen.getByLabelText(/förnamn/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/efternamn/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/e-post/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^lösenord$/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/bekräfta lösenord/i)).toBeInTheDocument();
  });

  it('should render submit button', () => {
    render(<RegisterForm />);

    expect(
      screen.getByRole('button', { name: /skapa konto/i })
    ).toBeInTheDocument();
  });

  it('should show validation error for invalid email', async () => {
    render(<RegisterForm />);

    const emailInput = screen.getByLabelText(/e-post/i);
    const submitButton = screen.getByRole('button', { name: /skapa konto/i });

    await user.type(emailInput, 'invalid-email');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/ogiltig e-postadress/i)).toBeInTheDocument();
    });
  });

  it('should show validation error for short password', async () => {
    render(<RegisterForm />);

    const passwordInput = screen.getByLabelText(/^lösenord$/i);
    const submitButton = screen.getByRole('button', { name: /skapa konto/i });

    await user.type(passwordInput, 'Ab1!');
    await user.click(submitButton);

    await waitFor(() => {
      expect(
        screen.getByText(/lösenordet måste vara minst 6 tecken/i)
      ).toBeInTheDocument();
    });
  });

  it('should show validation error for password without uppercase', async () => {
    render(<RegisterForm />);

    const passwordInput = screen.getByLabelText(/^lösenord$/i);
    const submitButton = screen.getByRole('button', { name: /skapa konto/i });

    await user.type(passwordInput, 'password123!');
    await user.click(submitButton);

    await waitFor(() => {
      expect(
        screen.getByText(/lösenordet måste innehålla minst en stor bokstav/i)
      ).toBeInTheDocument();
    });
  });

  it('should show validation error for password without lowercase', async () => {
    render(<RegisterForm />);

    const passwordInput = screen.getByLabelText(/^lösenord$/i);
    const submitButton = screen.getByRole('button', { name: /skapa konto/i });

    await user.type(passwordInput, 'PASSWORD123!');
    await user.click(submitButton);

    await waitFor(() => {
      expect(
        screen.getByText(/lösenordet måste innehålla minst en liten bokstav/i)
      ).toBeInTheDocument();
    });
  });

  it('should show validation error for password without number', async () => {
    render(<RegisterForm />);

    const passwordInput = screen.getByLabelText(/^lösenord$/i);
    const submitButton = screen.getByRole('button', { name: /skapa konto/i });

    await user.type(passwordInput, 'Password!');
    await user.click(submitButton);

    await waitFor(() => {
      expect(
        screen.getByText(/lösenordet måste innehålla minst en siffra/i)
      ).toBeInTheDocument();
    });
  });

  it('should show validation error for password without special character', async () => {
    render(<RegisterForm />);

    const passwordInput = screen.getByLabelText(/^lösenord$/i);
    const submitButton = screen.getByRole('button', { name: /skapa konto/i });

    await user.type(passwordInput, 'Password123');
    await user.click(submitButton);

    await waitFor(() => {
      expect(
        screen.getByText(/lösenordet måste innehålla minst ett specialtecken/i)
      ).toBeInTheDocument();
    });
  });

  it('should show validation error when passwords do not match', async () => {
    render(<RegisterForm />);

    const passwordInput = screen.getByLabelText(/^lösenord$/i);
    const confirmPasswordInput = screen.getByLabelText(/bekräfta lösenord/i);
    const submitButton = screen.getByRole('button', { name: /skapa konto/i });

    await user.type(passwordInput, 'Password123!');
    await user.type(confirmPasswordInput, 'DifferentPassword123!');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/lösenorden matchar inte/i)).toBeInTheDocument();
    });
  });

  it('should submit form with valid data', async () => {
    const mockResponse = createMockAuthResponse();
    vi.mocked(global.fetch).mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve(mockResponse),
    } as Response);

    const onSuccess = vi.fn();
    render(<RegisterForm onSuccess={onSuccess} />);

    await user.type(screen.getByLabelText(/förnamn/i), 'Test');
    await user.type(screen.getByLabelText(/efternamn/i), 'User');
    await user.type(screen.getByLabelText(/e-post/i), 'test@example.com');
    await user.type(screen.getByLabelText(/^lösenord$/i), 'Password123!');
    await user.type(
      screen.getByLabelText(/bekräfta lösenord/i),
      'Password123!'
    );

    await user.click(screen.getByRole('button', { name: /skapa konto/i }));

    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalled();
    });
  });

  it('should show error alert on failed registration', async () => {
    vi.mocked(global.fetch).mockResolvedValueOnce({
      ok: false,
      status: 409,
      json: () =>
        Promise.resolve({ message: 'En användare med denna e-post finns redan' }),
    } as Response);

    render(<RegisterForm />);

    await user.type(screen.getByLabelText(/e-post/i), 'existing@example.com');
    await user.type(screen.getByLabelText(/^lösenord$/i), 'Password123!');
    await user.type(
      screen.getByLabelText(/bekräfta lösenord/i),
      'Password123!'
    );

    await user.click(screen.getByRole('button', { name: /skapa konto/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
  });

  it('should toggle password visibility for both password fields', async () => {
    render(<RegisterForm />);

    const passwordInput = screen.getByLabelText(/^lösenord$/i);
    const confirmPasswordInput = screen.getByLabelText(/bekräfta lösenord/i);

    expect(passwordInput).toHaveAttribute('type', 'password');
    expect(confirmPasswordInput).toHaveAttribute('type', 'password');

    // Find the toggle button (there should be only one that affects both)
    const toggleButtons = screen.getAllByRole('button', { name: '' });
    const toggleButton = toggleButtons[0]; // First toggle button

    await user.click(toggleButton);

    expect(passwordInput).toHaveAttribute('type', 'text');
    expect(confirmPasswordInput).toHaveAttribute('type', 'text');
  });

  it('should disable submit button while loading', async () => {
    vi.mocked(global.fetch).mockImplementationOnce(
      () =>
        new Promise((resolve) =>
          setTimeout(
            () =>
              resolve({
                ok: true,
                status: 200,
                json: () => Promise.resolve(createMockAuthResponse()),
              } as Response),
            100
          )
        )
    );

    render(<RegisterForm />);

    await user.type(screen.getByLabelText(/e-post/i), 'test@example.com');
    await user.type(screen.getByLabelText(/^lösenord$/i), 'Password123!');
    await user.type(
      screen.getByLabelText(/bekräfta lösenord/i),
      'Password123!'
    );

    const submitButton = screen.getByRole('button', { name: /skapa konto/i });
    await user.click(submitButton);

    expect(submitButton).toBeDisabled();

    await waitFor(() => {
      expect(submitButton).not.toBeDisabled();
    });
  });

  it('should allow registration with only required fields', async () => {
    const mockResponse = createMockAuthResponse();
    vi.mocked(global.fetch).mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve(mockResponse),
    } as Response);

    const onSuccess = vi.fn();
    render(<RegisterForm onSuccess={onSuccess} />);

    // Only fill required fields (email, password, confirm password)
    await user.type(screen.getByLabelText(/e-post/i), 'test@example.com');
    await user.type(screen.getByLabelText(/^lösenord$/i), 'Password123!');
    await user.type(
      screen.getByLabelText(/bekräfta lösenord/i),
      'Password123!'
    );

    await user.click(screen.getByRole('button', { name: /skapa konto/i }));

    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalled();
    });
  });
});
