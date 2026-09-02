import { apiClient } from "@/lib/api-client";
import { STORAGE_KEYS } from "@/lib/constants";
import {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
  User,
} from "../types/auth.types";

function setAuthCookie(token: string) {
  if (typeof document !== "undefined") {
    // 7 days expiration
    document.cookie = `${STORAGE_KEYS.ACCESS_TOKEN}=${encodeURIComponent(token)}; path=/; max-age=604800; SameSite=Lax`;
  }
}

function clearAuthCookie() {
  if (typeof document !== "undefined") {
    document.cookie = `${STORAGE_KEYS.ACCESS_TOKEN}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT; SameSite=Lax`;
  }
}

export const authService = {
  async login(credentials: LoginRequest): Promise<{ user: User; tokens: LoginResponse }> {
    const tokens = await apiClient.post<LoginResponse>(
      "/api/auth/login",
      credentials
    );

    if (typeof window !== "undefined") {
      apiClient.setTokens(tokens.accessToken, tokens.refreshToken);
    }

    // Fetch live user profile from backend
    const user = await apiClient.get<User>("/api/users/me");

    if (typeof window !== "undefined") {
      localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(user));
    }

    return { user, tokens };
  },

  async register(data: RegisterRequest): Promise<RegisterResponse> {
    return apiClient.post<RegisterResponse>("/api/auth/register", data);
  },

  async refreshToken(): Promise<string | null> {
    return apiClient.refreshToken();
  },

  async getCurrentUser(): Promise<User | null> {
    if (typeof window === "undefined") return null;

    const token = localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN);
    if (!token) {
      clearAuthCookie();
      return null;
    }

    // Keep cookie synced
    setAuthCookie(token);

    try {
      const user = await apiClient.get<User>("/api/users/me");
      localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(user));
      return user;
    } catch {
      // If access token expired, attempt refresh
      const refreshedToken = await apiClient.refreshToken();
      if (refreshedToken) {
        try {
          const refreshedUser = await apiClient.get<User>("/api/users/me");
          localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(refreshedUser));
          return refreshedUser;
        } catch {
          this.logout();
          return null;
        }
      }
      this.logout();
      return null;
    }
  },

  logout(): void {
    if (typeof window === "undefined") return;
    apiClient.clearTokens();
  },
};
