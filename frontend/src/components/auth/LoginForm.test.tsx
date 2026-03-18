import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { render, createMockAuthResponse } from '../../test/test-utils';
import { LoginForm } from './LoginForm';
import { useAuthStore } from '../../stores/authStore';

describe('LoginForm', () => {
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

  it('should render email and password fields', () => {
    render(<LoginForm />);

    expect(screen.getByLabelText(/e-post/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/lösenord/i)).toBeInTheDocument();
  });

  it('should render submit button', () => {
    render(<LoginForm />);

    expect(
      screen.getByRole('button', { name: /logga in/i })
    ).toBeInTheDocument();
  });

  it('should show validation error for invalid email', async () => {
    render(<LoginForm />);

    const emailInput = screen.getByLabelText(/e-post/i);
    const submitButton = screen.getByRole('button', { name: /logga in/i });

    await user.type(emailInput, 'invalid-email');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/ogiltig e-postadress/i)).toBeInTheDocument();
    });
  });

  it('should show validation error when password is empty', async () => {
    render(<LoginForm />);

    const emailInput = screen.getByLabelText(/e-post/i);
    const submitButton = screen.getByRole('button', { name: /logga in/i });

    await user.type(emailInput, 'test@example.com');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/lösenord krävs/i)).toBeInTheDocument();
    });
  });

  it('should submit form with valid credentials', async () => {
    const mockResponse = createMockAuthResponse();
    vi.mocked(global.fetch).mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve(mockResponse),
    } as Response);

    const onSuccess = vi.fn();
    render(<LoginForm onSuccess={onSuccess} />);

    const emailInput = screen.getByLabelText(/e-post/i);
    const passwordInput = screen.getByLabelText(/lösenord/i);
    const submitButton = screen.getByRole('button', { name: /logga in/i });

    await user.type(emailInput, 'test@example.com');
    await user.type(passwordInput, 'Password123!');
    await user.click(submitButton);

    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalled();
    });
  });

  it('should show error alert on failed login', async () => {
    vi.mocked(global.fetch).mockResolvedValueOnce({
      ok: false,
      status: 401,
      json: () => Promise.resolve({ message: 'Ogiltigt e-post eller lösenord' }),
    } as Response);

    render(<LoginForm />);

    const emailInput = screen.getByLabelText(/e-post/i);
    const passwordInput = screen.getByLabelText(/lösenord/i);
    const submitButton = screen.getByRole('button', { name: /logga in/i });

    await user.type(emailInput, 'test@example.com');
    await user.type(passwordInput, 'WrongPassword!');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
  });

  it('should toggle password visibility', async () => {
    render(<LoginForm />);

    const passwordInput = screen.getByLabelText(/lösenord/i);
    expect(passwordInput).toHaveAttribute('type', 'password');

    const toggleButton = screen.getByRole('button', { name: '' });
    await user.click(toggleButton);

    expect(passwordInput).toHaveAttribute('type', 'text');

    await user.click(toggleButton);
    expect(passwordInput).toHaveAttribute('type', 'password');
  });

  it('should disable submit button while loading', async () => {
    // Mock a slow response
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

    render(<LoginForm />);

    const emailInput = screen.getByLabelText(/e-post/i);
    const passwordInput = screen.getByLabelText(/lösenord/i);
    const submitButton = screen.getByRole('button', { name: /logga in/i });

    await user.type(emailInput, 'test@example.com');
    await user.type(passwordInput, 'Password123!');
    await user.click(submitButton);

    // Button should be disabled during loading
    expect(submitButton).toBeDisabled();

    await waitFor(() => {
      expect(submitButton).not.toBeDisabled();
    });
  });

  it('should clear error when close button is clicked', async () => {
    // First, set an error state
    useAuthStore.setState({ error: 'Testfel' });

    render(<LoginForm />);

    const alert = screen.getByRole('alert');
    expect(alert).toBeInTheDocument();

    const closeButton = screen.getByRole('button', { name: /close/i });
    await user.click(closeButton);

    await waitFor(() => {
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });
  });
});
