import { apiClient } from './client';

export interface RegisterRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  deviceInfo?: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface UserInfo {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  emailConfirmed: boolean;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiration: string;
  user: UserInfo;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface MessageResponse {
  message: string;
}

export const authApi = {
  register: (request: RegisterRequest): Promise<AuthResponse> =>
    apiClient.post('/api/auth/register', request, { skipAuth: true }),

  login: (request: LoginRequest): Promise<AuthResponse> =>
    apiClient.post('/api/auth/login', request, { skipAuth: true }),

  refresh: (request: RefreshTokenRequest): Promise<AuthResponse> =>
    apiClient.post('/api/auth/refresh', request, { skipAuth: true }),

  logout: (refreshToken?: string): Promise<void> =>
    apiClient.post('/api/auth/logout', refreshToken ? { refreshToken } : undefined),

  logoutAll: (): Promise<void> =>
    apiClient.post('/api/auth/logout-all'),

  getCurrentUser: (): Promise<UserInfo> =>
    apiClient.get('/api/auth/me'),

  forgotPassword: (request: ForgotPasswordRequest): Promise<MessageResponse> =>
    apiClient.post('/api/auth/forgot-password', request, { skipAuth: true }),

  resetPassword: (request: ResetPasswordRequest): Promise<MessageResponse> =>
    apiClient.post('/api/auth/reset-password', request, { skipAuth: true }),
};
