"use client";

import React, { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  Mail,
  Lock,
  Eye,
  EyeOff,
  LogIn,
  AlertCircle,
} from "lucide-react";
import { useAuth } from "@/features/auth/hooks/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";

export default function LoginPage() {
  const router = useRouter();
  const { login, isLoading: isAuthLoading } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim() || !password.trim()) {
      setErrorMsg("Vui lòng điền đầy đủ Email và Mật khẩu.");
      return;
    }

    setErrorMsg(null);
    setIsSubmitting(true);

    try {
      await login({ email, password });
      router.push("/");
    } catch (err: unknown) {
      const error = err as { message?: string };
      setErrorMsg(
        error?.message || "Đăng nhập không thành công. Vui lòng kiểm tra lại tài khoản/mật khẩu."
      );
    } finally {
      setIsSubmitting(false);
    }
  };



  return (
    <div className="min-h-screen w-full flex flex-col justify-center items-center bg-[#F0F4F8] p-4 sm:p-6 lg:p-8">
      {/* Background soft ambient accents */}
      <div className="absolute top-1/4 left-1/3 size-96 rounded-full bg-blue-200/40 blur-3xl pointer-events-none" />
      <div className="absolute bottom-1/4 right-1/3 size-96 rounded-full bg-rose-200/30 blur-3xl pointer-events-none" />

      <div className="relative w-full max-w-md">
        {/* Card */}
        <div className="relative rounded-2xl bg-white p-6 sm:p-8 shadow-xl border border-slate-200/80 backdrop-blur-xs">
          {/* Header Brand */}
          <div className="text-center mb-6">
            <div className="inline-flex items-center justify-center gap-2 mb-2">
              <span className="font-black text-2xl tracking-tight text-[#0E1E4D]">
                TCIP
              </span>
              <div className="relative size-5 flex items-center justify-center">
                <span className="absolute -top-0.5 size-2 rounded-xs bg-[#0284C7]" />
                <span className="absolute -bottom-0.5 size-2 rounded-xs bg-[#EA580C]" />
                <span className="absolute -left-0.5 size-2 rounded-xs bg-[#16A34A]" />
                <span className="absolute -right-0.5 size-2 rounded-xs bg-[#DB2777]" />
              </div>
            </div>
            <h1 className="text-lg font-bold text-slate-800">
              Đăng nhập hệ thống
            </h1>
            <p className="text-xs text-slate-500 mt-1">
              Quản lý lịch làm việc, dự án và nhân sự nội bộ
            </p>
          </div>

          {/* Error message */}
          {errorMsg && (
            <div className="mb-4 flex items-start gap-2.5 rounded-lg bg-rose-50 border border-rose-200 p-3 text-xs text-rose-700 animate-in fade-in">
              <AlertCircle className="size-4 text-rose-500 shrink-0 mt-0.5" />
              <div className="flex-1">{errorMsg}</div>
            </div>
          )}

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Email công việc
              </label>
              <div className="relative">
                <Mail className="absolute left-3 top-2.5 size-4 text-slate-400" />
                <Input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="pl-9 h-10 text-sm"
                  required
                />
              </div>
            </div>

            <div>
              <div className="flex items-center justify-between mb-1">
                <label className="block text-xs font-semibold text-slate-700">
                  Mật khẩu
                </label>
                <a
                  href="#forgot"
                  onClick={(e) => {
                    e.preventDefault();
                    alert("Vui lòng liên hệ Quản trị viên để đặt lại mật khẩu!");
                  }}
                  className="text-xs text-blue-600 hover:underline"
                >
                  Quên mật khẩu?
                </a>
              </div>
              <div className="relative">
                <Lock className="absolute left-3 top-2.5 size-4 text-slate-400" />
                <Input
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="pl-9 pr-10 h-10 text-sm"
                  required
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-2.5 text-slate-400 hover:text-slate-600 cursor-pointer"
                >
                  {showPassword ? (
                    <EyeOff className="size-4" />
                  ) : (
                    <Eye className="size-4" />
                  )}
                </button>
              </div>
            </div>

            <div className="flex items-center justify-between pt-1">
              <div className="flex items-center gap-2">
                <Checkbox
                  id="remember-me"
                  checked={rememberMe}
                  onCheckedChange={(c) => setRememberMe(c)}
                />
                <label
                  htmlFor="remember-me"
                  className="text-xs text-slate-600 cursor-pointer select-none"
                >
                  Ghi nhớ đăng nhập
                </label>
              </div>
            </div>

            <Button
              type="submit"
              disabled={isSubmitting || isAuthLoading}
              className="w-full h-10 bg-[#0E1E4D] hover:bg-[#152a68] text-white font-semibold text-sm rounded-lg shadow-sm transition-all flex items-center justify-center gap-2 cursor-pointer"
            >
              {isSubmitting ? (
                <div className="size-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              ) : (
                <>
                  <LogIn className="size-4" />
                  <span>Đăng nhập</span>
                </>
              )}
            </Button>
          </form>

        

          {/* Register footer */}
          <div className="mt-6 text-center text-xs text-slate-500">
            Chưa có tài khoản?{" "}
            <Link
              href="/register"
              className="font-semibold text-blue-600 hover:underline"
            >
              Đăng ký tài khoản mới
            </Link>
          </div>
        </div>

        {/* Footer info */}
        <div className="text-center mt-4 text-[11px] text-slate-400">
          © 2025 TCIP Corporation. All rights reserved.
        </div>
      </div>
    </div>
  );
}
