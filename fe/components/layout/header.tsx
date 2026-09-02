"use client";

import React, { useState, useRef, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  Search,
  Bell,
  ChevronDown,
  Globe,
  LogOut,
  Check,
  Radio,
} from "lucide-react";
import { useAuth } from "@/features/auth/hooks/use-auth";
import { useNotifications } from "@/features/notifications/context/notification-context";
import { Avatar } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";

export function Header() {
  const router = useRouter();
  const { user, logout, isAuthenticated } = useAuth();
  const {
    notifications,
    unreadCount,
    isConnected,
    markAsRead,
    markAllAsRead,
  } = useNotifications();

  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const [isLangMenuOpen, setIsLangMenuOpen] = useState(false);
  const [isNotifOpen, setIsNotifOpen] = useState(false);
  const [currentLang, setCurrentLang] = useState("VI");

  const userMenuRef = useRef<HTMLDivElement>(null);
  const langMenuRef = useRef<HTMLDivElement>(null);
  const notifMenuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        userMenuRef.current &&
        !userMenuRef.current.contains(event.target as Node)
      ) {
        setIsUserMenuOpen(false);
      }
      if (
        langMenuRef.current &&
        !langMenuRef.current.contains(event.target as Node)
      ) {
        setIsLangMenuOpen(false);
      }
      if (
        notifMenuRef.current &&
        !notifMenuRef.current.contains(event.target as Node)
      ) {
        setIsNotifOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleLogout = () => {
    logout();
    router.push("/login");
  };

  return (
    <header className="sticky top-0 z-40 flex h-14 w-full items-center justify-between border-b border-slate-200 bg-white px-4 lg:px-6 shadow-2xs">
      {/* Brand / Logo */}
      <div className="flex items-center gap-3">
        <Link href="/" className="flex items-center gap-2 group">
          {/* TCIP Logo Mark */}
          <div className="flex items-center gap-1.5 font-bold tracking-tight text-xl text-slate-800">
            <span className="font-extrabold text-[#0E1E4D]">TCIP</span>
            {/* 4-Color decorative cross icon matching screenshot */}
            <div className="relative size-4 flex items-center justify-center">
              <span className="absolute -top-0.5 size-1.5 rounded-xs bg-[#0284C7]" />
              <span className="absolute -bottom-0.5 size-1.5 rounded-xs bg-[#EA580C]" />
              <span className="absolute -left-0.5 size-1.5 rounded-xs bg-[#16A34A]" />
              <span className="absolute -right-0.5 size-1.5 rounded-xs bg-[#DB2777]" />
            </div>
          </div>
        </Link>
      </div>

      {/* Right Controls */}
      <div className="flex items-center gap-2 sm:gap-3">
        {/* Search Bar matching screenshot "Tìm kiếm (Ctrl + K)" */}
        <div className="relative hidden md:flex items-center">
          <Search className="absolute left-2.5 size-4 text-slate-400 pointer-events-none" />
          <input
            type="text"
            placeholder="Tìm kiếm (Ctrl + K)"
            className="h-8 w-56 lg:w-64 rounded-md border border-slate-200 bg-slate-50/70 pl-8 pr-3 text-xs placeholder:text-slate-400 focus:border-blue-500 focus:bg-white focus:outline-hidden transition-all"
          />
        </div>

        {/* Language Selector */}
        <div className="relative" ref={langMenuRef}>
          <button
            type="button"
            onClick={() => setIsLangMenuOpen(!isLangMenuOpen)}
            className="flex h-8 items-center gap-1 rounded-md px-2 text-xs font-medium text-slate-600 hover:bg-slate-100 hover:text-slate-900 transition-colors cursor-pointer"
          >
            <Globe className="size-3.5 text-slate-500" />
            <span>{currentLang}</span>
            <ChevronDown className="size-3 text-slate-400" />
          </button>

          {isLangMenuOpen && (
            <div className="absolute right-0 mt-1 w-32 rounded-lg border border-slate-200 bg-white py-1 shadow-lg z-50 animate-in fade-in-50 zoom-in-95">
              <button
                type="button"
                onClick={() => {
                  setCurrentLang("VI");
                  setIsLangMenuOpen(false);
                }}
                className="flex w-full items-center justify-between px-3 py-1.5 text-xs text-slate-700 hover:bg-slate-50 hover:text-blue-600 cursor-pointer"
              >
                <span>Tiếng Việt (VI)</span>
                {currentLang === "VI" && <Check className="size-3 text-blue-600" />}
              </button>
              <button
                type="button"
                onClick={() => {
                  setCurrentLang("EN");
                  setIsLangMenuOpen(false);
                }}
                className="flex w-full items-center justify-between px-3 py-1.5 text-xs text-slate-700 hover:bg-slate-50 hover:text-blue-600 cursor-pointer"
              >
                <span>English (EN)</span>
                {currentLang === "EN" && <Check className="size-3 text-blue-600" />}
              </button>
            </div>
          )}
        </div>

        {/* Notifications Bell with SignalR realtime updates */}
        <div className="relative" ref={notifMenuRef}>
          <button
            type="button"
            onClick={() => setIsNotifOpen(!isNotifOpen)}
            className="relative flex size-8 items-center justify-center rounded-md text-slate-500 hover:bg-slate-100 hover:text-slate-800 transition-colors cursor-pointer"
            aria-label="Thông báo"
          >
            <Bell className="size-4" />
            {unreadCount > 0 && (
              <span className="absolute top-1 right-1 flex size-2.5">
                <span className="absolute inline-flex size-full animate-ping rounded-full bg-rose-400 opacity-75" />
                <span className="relative inline-flex size-2.5 rounded-full bg-rose-500 text-[8px] font-bold text-white items-center justify-center">
                  {unreadCount > 9 ? "9+" : unreadCount}
                </span>
              </span>
            )}
          </button>

          {isNotifOpen && (
            <div className="absolute right-0 mt-1 w-80 sm:w-96 rounded-xl border border-slate-200 bg-white shadow-2xl z-50 animate-in fade-in-50 zoom-in-95">
              <div className="flex items-center justify-between border-b border-slate-100 px-4 py-3">
                <div className="flex items-center gap-2">
                  <span className="font-bold text-xs text-slate-800">
                    Thông báo ({notifications.length})
                  </span>
                  {/* SignalR connection indicator */}
                  <span
                    className={`inline-flex items-center gap-1 text-[10px] font-medium px-1.5 py-0.5 rounded-full ${
                      isConnected
                        ? "bg-emerald-50 text-emerald-600 border border-emerald-200"
                        : "bg-amber-50 text-amber-600 border border-amber-200"
                    }`}
                    title={isConnected ? " đang kết nối trực tiếp" : "đang kết nối lại..."}
                  >
                    <span
                      className={`size-1.5 rounded-full ${
                        isConnected ? "bg-emerald-500 animate-pulse" : "bg-amber-400"
                      }`}
                    />
                    <span>{isConnected ? "" : ""}</span>
                  </span>
                </div>

                {unreadCount > 0 && (
                  <button
                    onClick={() => void markAllAsRead()}
                    className="text-[11px] font-semibold text-blue-600 hover:underline cursor-pointer"
                  >
                    Đánh dấu đã đọc
                  </button>
                )}
              </div>

              <div className="max-h-80 overflow-y-auto divide-y divide-slate-100">
                {notifications.length === 0 ? (
                  <div className="p-6 text-center text-xs text-slate-400">
                    Không có thông báo nào
                  </div>
                ) : (
                  notifications.map((n) => {
                    const isUnread = !n.readAt;
                    return (
                      <div
                        key={n.id}
                        onClick={() => {
                          if (isUnread) void markAsRead(n.id);
                        }}
                        className={`p-3.5 transition-colors cursor-pointer text-xs ${
                          isUnread ? "bg-blue-50/40 hover:bg-blue-50/70" : "hover:bg-slate-50"
                        }`}
                      >
                        <div className="flex items-start justify-between gap-2">
                          <div className="font-semibold text-slate-800 flex items-center gap-1.5">
                            {isUnread && (
                              <span className="size-1.5 rounded-full bg-blue-600 shrink-0" />
                            )}
                            <span>{n.title}</span>
                          </div>
                          <span className="text-[10px] text-slate-400 font-mono shrink-0">
                            {new Date(n.sentAt).toLocaleTimeString("vi-VN", {
                              hour: "2-digit",
                              minute: "2-digit",
                            })}
                          </span>
                        </div>
                        {n.description && (
                          <div className="text-slate-500 mt-1 text-[11px] leading-relaxed line-clamp-2">
                            {n.description}
                          </div>
                        )}
                      </div>
                    );
                  })
                )}
              </div>
            </div>
          )}
        </div>

        {/* User Profile Avatar & Dropdown */}
        <div className="relative" ref={userMenuRef}>
          {isAuthenticated ? (
            <button
              type="button"
              onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
              className="flex items-center gap-2 rounded-full p-0.5 hover:ring-2 hover:ring-slate-200 transition-all cursor-pointer"
            >
              <Avatar
                src={user?.avatar}
                fallback={user?.displayName || "User"}
                size="sm"
              />
            </button>
          ) : (
            <Link
              href="/login"
              className="px-3 py-1.5 text-xs font-semibold text-white bg-[#0E1E4D] hover:bg-[#182f6e] rounded-md shadow-xs"
            >
              Đăng nhập
            </Link>
          )}

          {isUserMenuOpen && user && (
            <div className="absolute right-0 mt-1 w-60 rounded-xl border border-slate-200 bg-white p-2 shadow-xl z-50">
              {/* User Info Header */}
              <div className="border-b border-slate-100 px-3 py-2">
                <p className="font-semibold text-xs text-slate-900 truncate">
                  {user.displayName || "Người dùng"}
                </p>
                <p className="text-[11px] text-slate-500 truncate">{user.email}</p>
                <div className="mt-1.5 flex items-center gap-1.5">
                  <Badge variant={user.role === "Admin" ? "default" : "secondary"}>
                    {user.role}
                  </Badge>
                </div>
              </div>

              <div className="pt-1">
                <button
                  type="button"
                  onClick={() => {
                    handleLogout();
                    setIsUserMenuOpen(false);
                  }}
                  className="flex w-full items-center gap-2 rounded-md px-3 py-1.5 text-xs font-medium text-rose-600 hover:bg-rose-50 cursor-pointer"
                >
                  <LogOut className="size-3.5" />
                  <span>Đăng xuất</span>
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
