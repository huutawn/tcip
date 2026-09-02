"use client";

import React, { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import { Header } from "@/components/layout/header";
import { Navbar } from "@/components/layout/navbar";
import { Sidebar } from "@/components/layout/sidebar";
import { useAuth } from "@/features/auth/context/auth-context";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const { isAuthenticated, isLoading } = useAuth();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      const redirectUrl =
        pathname === "/" ? "/login" : `/login?redirect=${encodeURIComponent(pathname)}`;
      router.replace(redirectUrl);
    }
  }, [isLoading, isAuthenticated, router, pathname]);

  return (
    <div className="min-h-screen flex flex-col bg-[#EBF1F6]">
      {/* Top Header */}
      <Header />

      {/* Main Tab Navigation */}
      <Navbar />

      {/* Main Work Area: Left Sidebar (Fixed) + Main Content (Right) */}
      <div className="flex-1 w-full px-4 lg:px-6 pb-6 flex flex-col md:flex-row gap-4 items-start">
        {/* Persistent Left Sidebar */}
        <Sidebar />

        {/* Dynamic Page Content */}
        <main className="flex-1 w-full min-w-0">
          {children}
        </main>
      </div>
    </div>
  );
}
