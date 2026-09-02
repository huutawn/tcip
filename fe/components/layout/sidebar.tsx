"use client";

import React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  CalendarDays,
  Briefcase,
  CalendarCheck,
  FolderKanban,
  Users,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface NavItem {
  label: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
  badge?: string | number;
}

export function Sidebar() {
  const pathname = usePathname();

  const navItems: NavItem[] = [
    {
      label: "Lịch sự kiện / Cuộc họp",
      href: "/",
      icon: CalendarCheck,
    },
    {
      label: "Các hoạt động",
      href: "/activities",
      icon: CalendarDays,
    },
    {
      label: "Công việc của tôi",
      href: "/my-tasks",
      icon: Briefcase,
    },

  ];

  return (
    <aside className="w-60 lg:w-64 shrink-0 flex flex-col gap-2 p-3 bg-white rounded-xl border border-slate-200/80 shadow-2xs self-start sticky top-3 select-none">
      <div className="px-2 py-1 mb-1">
        <span className="text-[11px] font-bold uppercase tracking-wider text-slate-400">
          Danh mục chính
        </span>
      </div>

      <nav className="flex flex-col gap-1">
        {navItems.map((item) => {
          const isActive =
            item.href === "/"
              ? pathname === "/"
              : pathname.startsWith(item.href);

          const Icon = item.icon;

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "group flex items-center justify-between gap-2.5 px-3 py-2.5 rounded-lg text-xs font-medium transition-all duration-150",
                isActive
                  ? "bg-[#0E1E4D] text-white shadow-xs font-semibold"
                  : "text-slate-600 hover:bg-slate-100/80 hover:text-slate-900"
              )}
            >
              <div className="flex items-center gap-2.5 min-w-0">
                <Icon
                  className={cn(
                    "size-4 shrink-0 transition-colors",
                    isActive ? "text-white" : "text-slate-400 group-hover:text-slate-600"
                  )}
                />
                <span className="truncate">{item.label}</span>
              </div>

              {item.badge !== undefined && (
                <span
                  className={cn(
                    "text-[10px] px-1.5 py-0.5 rounded-full font-bold shrink-0",
                    isActive
                      ? "bg-white/20 text-white"
                      : "bg-slate-200 text-slate-600"
                  )}
                >
                  {item.badge}
                </span>
              )}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
