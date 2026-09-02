"use client";

import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  useRef,
} from "react";
import { User, LoginRequest, RegisterRequest } from "../types/auth.types";
import { authService } from "../services/auth.service";
import { STORAGE_KEYS } from "@/lib/constants";

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
  refreshToken: () => Promise<string | null>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

function parseJwtExp(token: string): number | null {
  try {
    const parts = token.split(".");
    if (parts.length !== 3) return null;
    const payload = JSON.parse(atob(parts[1]));
    return typeof payload.exp === "number" ? payload.exp : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const refreshTimerRef = useRef<NodeJS.Timeout | null>(null);

  // Setup proactive background token refresh before expiry
  const scheduleTokenRefresh = useCallback(function scheduleTokenRefreshImpl() {
    if (typeof window === "undefined") return;

    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current);
      refreshTimerRef.current = null;
    }

    const token = localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN);
    if (!token) return;

    const exp = parseJwtExp(token);
    if (!exp) return;

    const nowSeconds = Math.floor(Date.now() / 1000);
    const secondsRemaining = exp - nowSeconds;

    // Refresh 60 seconds before expiration, or in 10 seconds if already close
    const refreshInSeconds = Math.max(10, secondsRemaining - 60);

    refreshTimerRef.current = setTimeout(async () => {
      try {
        const newToken = await authService.refreshToken();
        if (newToken) {
          scheduleTokenRefreshImpl();
        }
      } catch (err) {
        console.warn("Background token refresh failed:", err);
      }
    }, refreshInSeconds * 1000);
  }, []);

  const initAuth = useCallback(async () => {
    try {
      const currentUser = await authService.getCurrentUser();
      setUser(currentUser);
      if (currentUser) {
        scheduleTokenRefresh();
      }
    } catch {
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, [scheduleTokenRefresh]);

  useEffect(() => {
    initAuth();

    // Listen to multi-tab storage events to sync auth state across tabs
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === STORAGE_KEYS.ACCESS_TOKEN) {
        if (!e.newValue) {
          setUser(null);
        } else {
          initAuth();
        }
      }
    };

    window.addEventListener("storage", handleStorageChange);
    return () => {
      window.removeEventListener("storage", handleStorageChange);
      if (refreshTimerRef.current) {
        clearTimeout(refreshTimerRef.current);
      }
    };
  }, [initAuth]);

  const login = async (credentials: LoginRequest) => {
    setIsLoading(true);
    try {
      const res = await authService.login(credentials);
      setUser(res.user);
      scheduleTokenRefresh();
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (data: RegisterRequest) => {
    setIsLoading(true);
    try {
      await authService.register(data);
      // Automatically login after successful registration
      const res = await authService.login({
        email: data.email,
        password: data.password,
      });
      setUser(res.user);
      scheduleTokenRefresh();
    } finally {
      setIsLoading(false);
    }
  };

  const logout = () => {
    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current);
      refreshTimerRef.current = null;
    }
    authService.logout();
    setUser(null);
  };

  const manualRefreshToken = async () => {
    const token = await authService.refreshToken();
    if (token) {
      scheduleTokenRefresh();
    }
    return token;
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        register,
        logout,
        refreshToken: manualRefreshToken,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
