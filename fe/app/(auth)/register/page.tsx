"use client";

import React, { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  Mail,
  Lock,
  User as UserIcon,
  UserPlus,
  AlertCircle,
  CheckCircle2,
} from "lucide-react";
import { useAuth } from "@/features/auth/hooks/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export default function RegisterPage() {
  const router = useRouter();
  const { register } = useAuth();

  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!displayName.trim() || !email.trim() || !password.trim()) {
      setErrorMsg("Vui lòng điền đầy đủ các trường thông tin.");
      return;
    }
    if (password !== confirmPassword) {
      setErrorMsg("Mật khẩu xác nhận không khớp.");
      return;
    }
    if (password.length < 6) {
      setErrorMsg("Mật khẩu phải có ít nhất 6 ký tự.");
      return;
    }

    setErrorMsg(null);
    setIsSubmitting(true);

    try {
      await register({
        displayName,
        email,
        password,
        language: "vi",
        timeZoneId: "SE Asia Standard Time",
      });
      router.push("/");
    } catch (err: unknown) {
      const error = err as { message?: string };
      setErrorMsg(error?.message || "Đăng ký không thành công. Vui lòng thử lại.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen w-full flex flex-col justify-center items-center bg-[#F0F4F8] p-4 sm:p-6 lg:p-8">
      <div className="relative w-full max-w-md">
        <div className="relative rounded-2xl bg-white p-6 sm:p-8 shadow-xl border border-slate-200/80">
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
              Đăng ký tài khoản mới
            </h1>
            <p className="text-xs text-slate-500 mt-1">
              Tham gia hệ thống quản trị và cộng tác công việc TCIP
            </p>
          </div>

          {errorMsg && (
            <div className="mb-4 flex items-start gap-2.5 rounded-lg bg-rose-50 border border-rose-200 p-3 text-xs text-rose-700">
              <AlertCircle className="size-4 text-rose-500 shrink-0 mt-0.5" />
              <div className="flex-1">{errorMsg}</div>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-3.5">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Họ và tên
              </label>
              <div className="relative">
                <UserIcon className="absolute left-3 top-2.5 size-4 text-slate-400" />
                <Input
                  type="text"
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  placeholder="Nguyễn Văn A"
                  className="pl-9 h-10 text-sm"
                  required
                />
              </div>
            </div>

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
                  placeholder="nguyenvana@tcip.vn"
                  className="pl-9 h-10 text-sm"
                  required
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Mật khẩu
              </label>
              <div className="relative">
                <Lock className="absolute left-3 top-2.5 size-4 text-slate-400" />
                <Input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Tối thiểu 6 ký tự"
                  className="pl-9 h-10 text-sm"
                  required
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Xác nhận mật khẩu
              </label>
              <div className="relative">
                <Lock className="absolute left-3 top-2.5 size-4 text-slate-400" />
                <Input
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  placeholder="Nhập lại mật khẩu"
                  className="pl-9 h-10 text-sm"
                  required
                />
              </div>
            </div>

            <Button
              type="submit"
              disabled={isSubmitting}
              className="w-full h-10 bg-[#0E1E4D] hover:bg-[#152a68] text-white font-semibold text-sm rounded-lg shadow-sm transition-all flex items-center justify-center gap-2 cursor-pointer mt-4"
            >
              {isSubmitting ? (
                <div className="size-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              ) : (
                <>
                  <UserPlus className="size-4" />
                  <span>Đăng ký</span>
                </>
              )}
            </Button>
          </form>

          <div className="mt-6 text-center text-xs text-slate-500">
            Đã có tài khoản?{" "}
            <Link
              href="/login"
              className="font-semibold text-blue-600 hover:underline"
            >
              Đăng nhập ngay
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
