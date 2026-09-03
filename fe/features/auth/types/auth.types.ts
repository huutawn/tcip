export type UserRole = "User" | "Admin";

export interface User {
  id: string;
  principalId: string;
  email: string;
  displayName: string;
  emailVerified: boolean;
  language: string;
  timeZoneId: string;
  role: UserRole;
  createdAtUtc: string;
  avatar?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
  language?: string;
  timeZoneId?: string;
}

export interface RegisterResponse {
  id: string;
  principalId: string;
  email: string;
  displayName: string;
  role: UserRole;
  createdAtUtc: string;
}

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}
