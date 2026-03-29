/* eslint-disable react-refresh/only-export-components */
import { ReactElement, ReactNode } from 'react';
import { render, RenderOptions } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';

const theme = createTheme();

interface WrapperProps {
  children: ReactNode;
}

function AllTheProviders({ children }: WrapperProps) {
  return (
    <BrowserRouter>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        {children}
      </ThemeProvider>
    </BrowserRouter>
  );
}

function customRender(
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>
) {
  return render(ui, { wrapper: AllTheProviders, ...options });
}

// Re-export everything
export * from '@testing-library/react';
export { customRender as render };

// Test data factories
export const createMockUser = (overrides = {}) => ({
  id: 'test-user-id',
  email: 'test@example.com',
  firstName: 'Test',
  lastName: 'User',
  emailConfirmed: true,
  roles: ['User'],
  ...overrides,
});

export const createMockAuthResponse = (overrides = {}) => ({
  accessToken: 'mock-access-token',
  refreshToken: 'mock-refresh-token',
  accessTokenExpiration: new Date(Date.now() + 15 * 60 * 1000).toISOString(),
  user: createMockUser(),
  ...overrides,
});

// Mock fetch helper
export const mockFetch = (response: unknown, ok = true, status = 200) => {
  return vi.mocked(global.fetch).mockResolvedValueOnce({
    ok,
    status,
    json: () => Promise.resolve(response),
  } as Response);
};

export const mockFetchError = (error: unknown, status = 400) => {
  return vi.mocked(global.fetch).mockResolvedValueOnce({
    ok: false,
    status,
    json: () => Promise.resolve(error),
  } as Response);
};
