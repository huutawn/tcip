"use client";

import React from "react";
import { Activity, Clock, CheckCircle2, AlertCircle } from "lucide-react";
import { Badge } from "@/components/ui/badge";

export default function ActivitiesPage() {
  const activities = [
    {
      id: "a1",
      title: "Cập nhật tài liệu kỹ thuật API Identity",
      user: "Nguyễn Văn Admin",
      time: "10 phút trước",
      type: "update",
    },
    {
      id: "a2",
      title: "Tạo sự kiện mới: Phỏng vấn nhân viên mới",
      user: "Hoàng Nam",
      time: "45 phút trước",
      type: "calendar",
    },
    {
      id: "a3",
      title: "Gán quyền Admin cho thành viên mới",
      user: "Nguyễn Văn Admin",
      time: "2 giờ trước",
      type: "security",
    },
  ];

  return (
    <div className="w-full bg-white rounded-xl border border-slate-200 p-6 shadow-2xs">
      <div className="pb-6 border-b border-slate-100">
        <h1 className="text-xl font-bold text-slate-800 flex items-center gap-2">
          <Activity className="size-5 text-[#0E1E4D]" />
          <span>Nhật ký Hoạt động (Activity Stream)</span>
        </h1>
        <p className="text-xs text-slate-500 mt-1">
          Theo dõi các thay đổi và hoạt động gần đây của toàn hệ thống
        </p>
      </div>

      <div className="divide-y divide-slate-100 mt-4">
        {activities.map((act) => (
          <div
            key={act.id}
            className="py-3.5 flex items-center justify-between gap-3 text-xs hover:bg-slate-50/70 px-2 rounded-lg transition-colors"
          >
            <div className="flex items-center gap-3">
              <div className="size-8 rounded-full bg-blue-50 text-blue-600 flex items-center justify-center font-bold">
                <CheckCircle2 className="size-4" />
              </div>
              <div>
                <p className="font-semibold text-slate-800">{act.title}</p>
                <p className="text-slate-500 text-[11px]">Thực hiện bởi <span className="font-medium text-slate-700">{act.user}</span></p>
              </div>
            </div>

            <div className="flex items-center gap-1.5 text-slate-400 text-[11px] shrink-0">
              <Clock className="size-3" />
              <span>{act.time}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
