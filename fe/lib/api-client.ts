import { STORAGE_KEYS, API_BASE_URL } from "./constants";

export interface ApiError {
  status: number;
  message: string;
  errors?: Record<string, string[]>;
}

class ApiClient {
  private baseUrl: string;
  private refreshPromise: Promise<string | null> | null = null;

  constructor() {
    this.baseUrl = API_BASE_URL;
  }

  getAuthToken(): string | null {
    if (typeof window === "undefined") return null;
    return localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN);
  }

  getRefreshToken(): string | null {
    if (typeof window === "undefined") return null;
    return localStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN);
  }

  setTokens(accessToken: string, refreshToken: string) {
    if (typeof window === "undefined") return;
    localStorage.setItem(STORAGE_KEYS.ACCESS_TOKEN, accessToken);
    localStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, refreshToken);
    document.cookie = `${STORAGE_KEYS.ACCESS_TOKEN}=${encodeURIComponent(accessToken)}; path=/; max-age=604800; SameSite=Lax`;
  }

  clearTokens() {
    if (typeof window === "undefined") return;
    localStorage.removeItem(STORAGE_KEYS.ACCESS_TOKEN);
    localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN);
    localStorage.removeItem(STORAGE_KEYS.USER);
    document.cookie = `${STORAGE_KEYS.ACCESS_TOKEN}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT; SameSite=Lax`;
  }

  private redirectToLogin() {
    if (typeof window !== "undefined" && !window.location.pathname.startsWith("/login")) {
      const currentPath = window.location.pathname;
      const target = currentPath === "/" ? "/login" : `/login?redirect=${encodeURIComponent(currentPath)}`;
      window.location.href = target;
    }
  }

  /**
   * Thread-safe Single-Flight Token Refresh
   * Multiple concurrent 401s will await the SAME in-flight refresh request
   */
  async refreshToken(): Promise<string | null> {
    if (this.refreshPromise) {
      return this.refreshPromise;
    }

    const currentRefreshToken = this.getRefreshToken();
    if (!currentRefreshToken) {
      this.clearTokens();
      return null;
    }

    this.refreshPromise = (async () => {
      try {
        const res = await fetch(`${this.baseUrl}/api/auth/refresh`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ refreshToken: currentRefreshToken }),
        });

        if (!res.ok) {
          this.clearTokens();
          return null;
        }

        const data = await res.json();
        if (data?.accessToken && data?.refreshToken) {
          this.setTokens(data.accessToken, data.refreshToken);
          return data.accessToken as string;
        }

        this.clearTokens();
        return null;
      } catch (error) {
        console.error("Token refresh failed:", error);
        this.clearTokens();
        return null;
      } finally {
        this.refreshPromise = null;
      }
    })();

    return this.refreshPromise;
  }

  async request<T>(
    endpoint: string,
    options: RequestInit = {},
    isRetry = false
  ): Promise<T> {
    const url = endpoint.startsWith("http")
      ? endpoint
      : `${this.baseUrl}${endpoint.startsWith("/") ? "" : "/"}${endpoint}`;

    const headers: Record<string, string> = {
      "Content-Type": "application/json",
      ...(options.headers as Record<string, string>),
    };

    const token = this.getAuthToken();
    if (token) {
      headers["Authorization"] = `Bearer ${token}`;
    }

    try {
      const response = await fetch(url, {
        ...options,
        headers,
      });

      // Handle 401 Unauthorized
      if (response.status === 401) {
        // If not already retried and not an auth route, try refreshing token
        if (!isRetry && !endpoint.includes("/api/auth/")) {
          const newAccessToken = await this.refreshToken();
          if (newAccessToken) {
            return this.request<T>(endpoint, options, true);
          }
        }

        // If refresh failed or already retried, clear tokens and redirect to login
        if (!endpoint.includes("/api/auth/login")) {
          this.clearTokens();
          this.redirectToLogin();
        }
      }

      if (response.status === 204) {
        return null as unknown as T;
      }

      const contentType = response.headers.get("content-type");
      const isJson = contentType && contentType.includes("application/json");
      const data = isJson ? await response.json() : await response.text();

      if (!response.ok) {
        const error: ApiError = {
          status: response.status,
          message:
            typeof data === "object" && data?.title
              ? data.title
              : typeof data === "object" && data?.message
              ? data.message
              : response.statusText || "An unexpected error occurred",
          errors: typeof data === "object" ? data?.errors : undefined,
        };
        throw error;
      }

      return data as T;
    } catch (err: unknown) {
      const errorObj = err as ApiError;
      if (errorObj?.status) {
        throw errorObj;
      }
      throw {
        status: 500,
        message:
          err instanceof Error
            ? err.message
            : "Network error. Please check your backend connection.",
      } as ApiError;
    }
  }

  get<T>(endpoint: string, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, { ...options, method: "GET" });
  }

  post<T>(endpoint: string, body?: unknown, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: "POST",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  }

  put<T>(endpoint: string, body?: unknown, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: "PUT",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  }

  delete<T>(endpoint: string, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, { ...options, method: "DELETE" });
  }
}

export const apiClient = new ApiClient();
