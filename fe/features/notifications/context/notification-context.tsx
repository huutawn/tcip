"use client";

import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  useRef,
} from "react";
import { NotificationResponse } from "@/features/calendar/types/calendar.types";
import { calendarService } from "@/features/calendar/services/calendar.service";
import { useAuth } from "@/features/auth/context/auth-context";
import { STORAGE_KEYS } from "@/lib/constants";
import { NotificationHubService } from "@/features/notifications/services/notification-hub.service";

interface NotificationContextType {
  notifications: NotificationResponse[];
  unreadCount: number;
  isConnected: boolean;
  latestNotification: NotificationResponse | null;
  markAsRead: (id: string) => Promise<void>;
  markAllAsRead: () => Promise<void>;
  dismissLatestNotification: () => void;
  refreshNotifications: () => Promise<void>;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  const [notifications, setNotifications] = useState<NotificationResponse[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isConnected, setIsConnected] = useState(false);
  const [latestNotification, setLatestNotification] = useState<NotificationResponse | null>(null);
  const notificationIdsRef = useRef(new Set<string>());

  const [notificationHub] = useState(() => new NotificationHubService());
  const toastTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const loadNotifications = useCallback(async () => {
    if (!isAuthenticated) {
      setNotifications([]);
      setUnreadCount(0);
      return;
    }

    try {
      const items = await calendarService.getNotifications();
      notificationIdsRef.current = new Set(items.map((item) => item.id));
      setNotifications(items);
      const unread = items.filter((n) => !n.readAt).length;
      setUnreadCount(unread);
    } catch {
      // Ignored
    }
  }, [isAuthenticated]);

  const handleNotification = useCallback((notification: NotificationResponse) => {
    const alreadyKnown = notificationIdsRef.current.has(notification.id);
    notificationIdsRef.current.add(notification.id);
    setNotifications((previous) => {
      return [notification, ...previous.filter((item) => item.id !== notification.id)];
    });
    if (!alreadyKnown && !notification.readAt) {
      setUnreadCount((count) => count + 1);
    }
    setLatestNotification(notification);

    if (toastTimeoutRef.current) {
      clearTimeout(toastTimeoutRef.current);
    }

    toastTimeoutRef.current = setTimeout(() => {
      setLatestNotification((current) =>
        current?.id === notification.id ? null : current
      );
    }, 8_000);
  }, []);

  useEffect(() => {
    let isActive = true;

    if (isAuthenticated) {
      void (async () => {
        await notificationHub.connect({
          accessTokenFactory: () => localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN) ?? "",
          onNotification: (notification) => {
            if (isActive) handleNotification(notification);
          },
          onConnectionStateChange: (connected) => {
            if (isActive) setIsConnected(connected);
          },
          onReconnected: () => {
            if (isActive) void loadNotifications();
          },
          onConnectionError: (error) => {
            console.warn("SignalR notification hub connection failed:", error);
          },
        });

        if (isActive) {
          await loadNotifications();
        }
      })();
    } else {
      void notificationHub.disconnect().then(() => {
        if (isActive) {
          setIsConnected(false);
          setLatestNotification(null);
        }
      });
    }

    return () => {
      isActive = false;
      if (toastTimeoutRef.current) {
        clearTimeout(toastTimeoutRef.current);
        toastTimeoutRef.current = null;
      }
      void notificationHub.disconnect();
    };
  }, [handleNotification, isAuthenticated, loadNotifications, notificationHub]);

  const markAsRead = async (id: string) => {
    try {
      await calendarService.markNotificationRead(id);
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, readAt: new Date().toISOString() } : n))
      );
      setUnreadCount((prev) => Math.max(0, prev - 1));
    } catch {
      // Ignored
    }
  };

  const markAllAsRead = async () => {
    try {
      const unreadList = notifications.filter((n) => !n.readAt);
      await Promise.all(unreadList.map((n) => calendarService.markNotificationRead(n.id).catch(() => {})));
      const nowIso = new Date().toISOString();
      setNotifications((prev) => prev.map((n) => ({ ...n, readAt: n.readAt || nowIso })));
      setUnreadCount(0);
    } catch {
      setUnreadCount(0);
    }
  };

  const dismissLatestNotification = () => {
    setLatestNotification(null);
  };

  return (
    <NotificationContext.Provider
      value={{
        notifications,
        unreadCount,
        isConnected,
        latestNotification,
        markAsRead,
        markAllAsRead,
        dismissLatestNotification,
        refreshNotifications: loadNotifications,
      }}
    >
      {children}

      {/* Real-time Floating Toast Alert for incoming Reminders */}
      {latestNotification && (
        <div className="fixed top-4 right-4 z-50 max-w-sm w-full bg-white rounded-xl shadow-2xl border border-blue-100 p-4 animate-in slide-in-from-top-4 duration-300">
          <div className="flex items-start justify-between gap-3">
            <div className="flex items-center gap-2 text-xs font-bold text-[#0E1E4D]">
              <span className="size-2 rounded-full bg-emerald-500 animate-pulse" />
              <span>Nhắc nhở cuộc họp / sự kiện</span>
            </div>
            <button
              onClick={dismissLatestNotification}
              className="text-slate-400 hover:text-slate-600 text-xs font-semibold"
            >
              ✕
            </button>
          </div>
          <h4 className="text-xs font-bold text-slate-800 mt-2">
            {latestNotification.title}
          </h4>
          {latestNotification.description && (
            <p className="text-[11px] text-slate-500 mt-1 line-clamp-2">
              {latestNotification.description}
            </p>
          )}
          <div className="mt-3 flex items-center justify-end gap-2">
            <button
              onClick={() => {
                void markAsRead(latestNotification.id);
                dismissLatestNotification();
              }}
              className="text-[11px] font-semibold text-blue-600 hover:underline cursor-pointer"
            >
              Đã xem
            </button>
          </div>
        </div>
      )}
    </NotificationContext.Provider>
  );
}

export function useNotifications(): NotificationContextType {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error("useNotifications must be used within a NotificationProvider");
  }
  return context;
}
