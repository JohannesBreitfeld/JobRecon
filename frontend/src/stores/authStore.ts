import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { authApi, type AuthResponse, type LoginRequest, type RegisterRequest, type UserInfo } from '../api/auth';
import { setSessionExpiredCallback } from '../api/client';

interface AuthState {
  user: UserInfo | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;

  login: (request: LoginRequest) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
  clearError: () => void;
}

const setTokens = (response: AuthResponse): void => {
  localStorage.setItem('accessToken', response.accessToken);
  localStorage.setItem('refreshToken', response.refreshToken);
};

const clearTokens = (): void => {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
};

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,

      login: async (request: LoginRequest) => {
        set({ isLoading: true, error: null });
        try {
          const response = await authApi.login(request);
          setTokens(response);
          set({
            user: response.user,
            isAuthenticated: true,
            isLoading: false,
          });
        } catch (err) {
          const message = err instanceof Error ? err.message : 'Inloggningen misslyckades.';
          set({ error: message, isLoading: false });
          throw err;
        }
      },

      register: async (request: RegisterRequest) => {
        set({ isLoading: true, error: null });
        try {
          const response = await authApi.register(request);
          setTokens(response);
          set({
            user: response.user,
            isAuthenticated: true,
            isLoading: false,
          });
        } catch (err) {
          const message = err instanceof Error ? err.message : 'Registreringen misslyckades.';
          set({ error: message, isLoading: false });
          throw err;
        }
      },

      logout: async () => {
        try {
          const refreshToken = localStorage.getItem('refreshToken');
          if (refreshToken) {
            await authApi.logout(refreshToken);
          }
        } catch {
          // Ignore errors during logout
        } finally {
          clearTokens();
          set({
            user: null,
            isAuthenticated: false,
            error: null,
          });
        }
      },

      refreshUser: async () => {
        const token = localStorage.getItem('accessToken');
        if (!token) {
          set({ user: null, isAuthenticated: false });
          return;
        }

        try {
          const user = await authApi.getCurrentUser();
          set({ user, isAuthenticated: true });
        } catch {
          clearTokens();
          set({ user: null, isAuthenticated: false });
        }
      },

      clearError: () => set({ error: null }),
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);

// When the API client detects an expired session (refresh failed),
// reset the Zustand store so the UI reflects the logged-out state.
setSessionExpiredCallback(() => {
  useAuthStore.setState({ user: null, isAuthenticated: false, error: null });
});
