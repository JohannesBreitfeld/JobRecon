import { describe, it, expect, vi, beforeEach } from 'vitest';
import { useAuthStore } from './authStore';
import { createMockAuthResponse, createMockUser } from '../test/test-utils';

describe('authStore', () => {
  beforeEach(() => {
    // Reset store state
    useAuthStore.setState({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,
    });

    // Clear mocks
    vi.clearAllMocks();
    localStorage.clear();
  });

  describe('initial state', () => {
    it('should have null user initially', () => {
      const { user } = useAuthStore.getState();
      expect(user).toBeNull();
    });

    it('should not be authenticated initially', () => {
      const { isAuthenticated } = useAuthStore.getState();
      expect(isAuthenticated).toBe(false);
    });

    it('should not be loading initially', () => {
      const { isLoading } = useAuthStore.getState();
      expect(isLoading).toBe(false);
    });

    it('should have no error initially', () => {
      const { error } = useAuthStore.getState();
      expect(error).toBeNull();
    });
  });

  describe('login', () => {
    it('should set isLoading to true during login', async () => {
      const mockResponse = createMockAuthResponse();
      vi.mocked(global.fetch).mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve(mockResponse),
      } as Response);

      const loginPromise = useAuthStore.getState().login({
        email: 'test@example.com',
        password: 'Password123!',
      });

      // Check loading state immediately after starting login
      expect(useAuthStore.getState().isLoading).toBe(true);

      await loginPromise;
    });

    it('should set user and isAuthenticated on successful login', async () => {
      const mockUser = createMockUser();
      const mockResponse = createMockAuthResponse({ user: mockUser });

      vi.mocked(global.fetch).mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve(mockResponse),
      } as Response);

      await useAuthStore.getState().login({
        email: 'test@example.com',
        password: 'Password123!',
      });

      const state = useAuthStore.getState();
      expect(state.user).toEqual(mockUser);
      expect(state.isAuthenticated).toBe(true);
      expect(state.isLoading).toBe(false);
      expect(state.error).toBeNull();
    });

    it('should store tokens in localStorage on successful login', async () => {
      const mockResponse = createMockAuthResponse();

      vi.mocked(global.fetch).mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve(mockResponse),
      } as Response);

      await useAuthStore.getState().login({
        email: 'test@example.com',
        password: 'Password123!',
      });

      expect(localStorage.setItem).toHaveBeenCalledWith(
        'accessToken',
        mockResponse.accessToken
      );
      expect(localStorage.setItem).toHaveBeenCalledWith(
        'refreshToken',
        mockResponse.refreshToken
      );
    });

    it('should set error on failed login', async () => {
      vi.mocked(global.fetch).mockResolvedValueOnce({
        ok: false,
        status: 401,
        json: () => Promise.resolve({ message: 'Ogiltiga inloggningsuppgifter' }),
      } as Response);

      await expect(
        useAuthStore.getState().login({
          email: 'test@example.com',
          password: 'WrongPassword!',
        })
      ).rejects.toThrow();

      const state = useAuthStore.getState();
      expect(state.user).toBeNull();
      expect(state.isAuthenticated).toBe(false);
      expect(state.isLoading).toBe(false);
      expect(state.error).toBe('Ogiltiga inloggningsuppgifter');
    });

    it('should set default error message when login fails without message', async () => {
      vi.mocked(global.fetch).mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: () => Promise.resolve({}),
      } as Response);

      await expect(
        useAuthStore.getState().login({
          email: 'test@example.com',
          password: 'Password123!',
        })
      ).rejects.toThrow();

      const state = useAuthStore.getState();
      expect(state.error).toBe('Ett fel uppstod');
    });
  });

  describe('register', () => {
    it('should set user and isAuthenticated on successful registration', async () => {
      const mockUser = createMockUser({ email: 'newuser@example.com' });
      const mockResponse = createMockAuthResponse({ user: mockUser });

      vi.mocked(global.fetch).mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve(mockResponse),
      } as Response);

      await useAuthStore.getState().register({
        email: 'newuser@example.com',
        password: 'Password123!',
        firstName: 'New',
        lastName: 'User',
      });

      const state = useAuthStore.getState();
      expect(state.user).toEqual(mockUser);
      expect(state.isAuthenticated).toBe(true);
    });

    it('should set error when registration fails', async () => {
      vi.mocked(global.fetch).mockResolvedValueOnce({
        ok: false,
        status: 409,
        json: () =>
          Promise.resolve({ message: 'En användare med denna e-post finns redan' }),
      } as Response);

      await expect(
        useAuthStore.getState().register({
          email: 'existing@example.com',
          password: 'Password123!',
        })
      ).rejects.toThrow();

      const state = useAuthStore.getState();
      expect(state.error).toBe('En användare med denna e-post finns redan');
    });
  });

  describe('logout', () => {
    it('should clear user and tokens on logout', async () => {
      // Setup authenticated state
      useAuthStore.setState({
        user: createMockUser(),
        isAuthenticated: true,
      });

      vi.mocked(localStorage.getItem).mockReturnValue('mock-refresh-token');
      vi.mocked(global.fetch).mockResolvedValueOnce({
        ok: true,
        status: 204,
        json: () => Promise.resolve(undefined),
      } as Response);

      await useAuthStore.getState().logout();

      const state = useAuthStore.getState();
      expect(state.user).toBeNull();
      expect(state.isAuthenticated).toBe(false);
      expect(localStorage.removeItem).toHaveBeenCalledWith('accessToken');
      expect(localStorage.removeItem).toHaveBeenCalledWith('refreshToken');
    });

    it('should clear state even if logout API call fails', async () => {
      // Setup authenticated state
      useAuthStore.setState({
        user: createMockUser(),
        isAuthenticated: true,
      });

      vi.mocked(localStorage.getItem).mockReturnValue('mock-refresh-token');
      vi.mocked(global.fetch).mockRejectedValueOnce(new Error('Network error'));

      await useAuthStore.getState().logout();

      const state = useAuthStore.getState();
      expect(state.user).toBeNull();
      expect(state.isAuthenticated).toBe(false);
    });
  });

  describe('clearError', () => {
    it('should clear the error', () => {
      useAuthStore.setState({ error: 'Some error' });

      useAuthStore.getState().clearError();

      expect(useAuthStore.getState().error).toBeNull();
    });
  });

  describe('refreshUser', () => {
    it('should update user when token exists', async () => {
      const mockUser = createMockUser();
      vi.mocked(localStorage.getItem).mockReturnValue('valid-token');
      vi.mocked(global.fetch).mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve(mockUser),
      } as Response);

      await useAuthStore.getState().refreshUser();

      const state = useAuthStore.getState();
      expect(state.user).toEqual(mockUser);
      expect(state.isAuthenticated).toBe(true);
    });

    it('should clear state when no token exists', async () => {
      vi.mocked(localStorage.getItem).mockReturnValue(null);

      await useAuthStore.getState().refreshUser();

      const state = useAuthStore.getState();
      expect(state.user).toBeNull();
      expect(state.isAuthenticated).toBe(false);
    });

    it('should clear state when refresh fails', async () => {
      vi.mocked(localStorage.getItem).mockReturnValue('expired-token');
      vi.mocked(global.fetch).mockResolvedValueOnce({
        ok: false,
        status: 401,
        json: () => Promise.resolve({ message: 'Unauthorized' }),
      } as Response);

      await useAuthStore.getState().refreshUser();

      const state = useAuthStore.getState();
      expect(state.user).toBeNull();
      expect(state.isAuthenticated).toBe(false);
      expect(localStorage.removeItem).toHaveBeenCalledWith('accessToken');
      expect(localStorage.removeItem).toHaveBeenCalledWith('refreshToken');
    });
  });
});
