"use client";

import React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";

interface NavTabItem {
  label: string;
  href: string;
  matchPrefix?: string;
}

const NAV_TABS: NavTabItem[] = [
  {
    label: "Dự án",
    href: "/projects",
  },
  {
    label: "Hoạt động mới",
    href: "/",
  },
  {
    label: "Quản lý nhân sự",
    href: "/hr",
  },
];

export function Navbar() {
  const pathname = usePathname();

  return (
    <nav className="w-full bg-[#EBF1F6] px-4 lg:px-6 pt-3 pb-1 select-none">
      <div className="flex w-full items-center justify-between overflow-hidden rounded-t-xl bg-white shadow-2xs border-b border-slate-200">
        {NAV_TABS.map((tab) => {
          const isActive =
            tab.href === "/"
              ? pathname === "/" || pathname === "/my-tasks" || pathname === "/activities"
              : pathname.startsWith(tab.href);

          return (
            <Link
              key={tab.href}
              href={tab.href}
              className={cn(
                "flex-1 py-3 text-center text-sm font-semibold transition-all duration-200 flex items-center justify-center gap-2",
                isActive
                  ? "bg-[#0E1E4D] text-white shadow-inner"
                  : "bg-transparent text-slate-700 hover:bg-slate-50 hover:text-slate-900"
              )}
            >
              {tab.label}
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
