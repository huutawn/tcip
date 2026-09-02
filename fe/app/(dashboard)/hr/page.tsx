"use client";

import React, { useState, useEffect } from "react";
import {
  Users,
  ShieldAlert,
  Search,
  CheckCircle2,
} from "lucide-react";
import { userService } from "@/features/users/services/user.service";
import { User, UserRole } from "@/features/auth/types/auth.types";
import { useAuth } from "@/features/auth/context/auth-context";
import { Avatar } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";

export default function HRManagementPage() {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState<User[]>([]);
  const [search, setSearch] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isForbidden, setIsForbidden] = useState(false);

  useEffect(() => {
    userService
      .getUsers()
      .then((res) => {
        setUsers(res?.items || []);
        setIsLoading(false);
      })
      .catch((err: { status?: number }) => {
        if (err?.status === 403) {
          setIsForbidden(true);
        }
        setIsLoading(false);
      });
  }, []);

  const handleRoleChange = async (userId: string, newRole: UserRole) => {
    try {
      await userService.updateUserRole(userId, newRole);
      setUsers((prev) =>
        prev.map((u) => (u.id === userId ? { ...u, role: newRole } : u))
      );
    } catch {
      alert("Bạn không có quyền cập nhật vai trò người dùng.");
    }
  };

  const filteredUsers = users.filter(
    (u) =>
      u.displayName.toLowerCase().includes(search.toLowerCase()) ||
      u.email.toLowerCase().includes(search.toLowerCase())
  );

  if (isForbidden || (currentUser && currentUser.role !== "Admin" && !isLoading && users.length === 0)) {
    return (
      <div className="w-full bg-white rounded-xl border border-slate-200 p-8 shadow-2xs text-center">
        <div className="size-12 rounded-full bg-amber-50 text-amber-600 flex items-center justify-center mx-auto mb-3">
          <ShieldAlert className="size-6" />
        </div>
        <h2 className="text-base font-bold text-slate-800">
          Quyền truy cập bị từ chối (403 Forbidden)
        </h2>
        <p className="text-xs text-slate-500 mt-1 max-w-sm mx-auto">
          Trang Quản lý nhân sự và Phân quyền chỉ dành riêng cho tài khoản có vai trò Quản trị viên (Admin).
        </p>
      </div>
    );
  }

  return (
    <div className="w-full bg-white rounded-xl border border-slate-200 p-6 shadow-2xs">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-slate-100">
        <div>
          <h1 className="text-xl font-bold text-slate-800 flex items-center gap-2">
            <Users className="size-5 text-[#0E1E4D]" />
            <span>Quản lý nhân sự & Phân quyền</span>
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Danh sách nhân viên, phòng ban và quyền hạn truy cập hệ thống
          </p>
        </div>

        <div className="flex items-center gap-3">
          <div className="relative w-64">
            <Search className="absolute left-2.5 top-2.5 size-4 text-slate-400" />
            <Input
              type="text"
              placeholder="Tìm kiếm nhân viên..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-8 text-xs h-9"
            />
          </div>
        </div>
      </div>

      {/* Users Table */}
      <div className="overflow-x-auto mt-4">
        <table className="w-full text-left text-xs border-collapse">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50/70 text-slate-600 font-semibold uppercase tracking-wider text-[11px]">
              <th className="py-3 px-4">Nhân viên</th>
              <th className="py-3 px-4">Email</th>
              <th className="py-3 px-4">Trạng thái</th>
              <th className="py-3 px-4">Vai trò (Role)</th>
              <th className="py-3 px-4 text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {isLoading ? (
              <tr>
                <td colSpan={5} className="py-8 text-center text-slate-400">
                  Đang tải danh sách nhân sự...
                </td>
              </tr>
            ) : filteredUsers.length === 0 ? (
              <tr>
                <td colSpan={5} className="py-8 text-center text-slate-400">
                  Không tìm thấy nhân viên phù hợp
                </td>
              </tr>
            ) : (
              filteredUsers.map((user) => (
                <tr
                  key={user.id}
                  className="hover:bg-slate-50/60 transition-colors"
                >
                  <td className="py-3 px-4 flex items-center gap-3">
                    <Avatar
                      src={user.avatar}
                      fallback={user.displayName}
                      size="sm"
                    />
                    <div className="font-semibold text-slate-800">
                      {user.displayName}
                    </div>
                  </td>
                  <td className="py-3 px-4 text-slate-600 font-mono">
                    {user.email}
                  </td>
                  <td className="py-3 px-4">
                    <span className="inline-flex items-center gap-1 text-[11px] text-emerald-600 font-medium bg-emerald-50 px-2 py-0.5 rounded-full">
                      <CheckCircle2 className="size-3" /> Hoạt động
                    </span>
                  </td>
                  <td className="py-3 px-4">
                    <Badge
                      variant={user.role === "Admin" ? "default" : "secondary"}
                    >
                      {user.role}
                    </Badge>
                  </td>
                  <td className="py-3 px-4 text-right">
                    <select
                      value={user.role}
                      onChange={(e) =>
                        handleRoleChange(user.id, e.target.value as UserRole)
                      }
                      className="h-7 text-xs rounded-md border border-slate-200 bg-white px-2 text-slate-700 cursor-pointer focus:border-blue-500 focus:outline-hidden"
                    >
                      <option value="User">Nhân viên (User)</option>
                      <option value="Admin">Quản trị (Admin)</option>
                    </select>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
