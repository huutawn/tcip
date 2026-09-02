"use client";

import React from "react";
import { FolderKanban, Plus, Clock, Users, CheckCircle2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";

export default function ProjectsPage() {
  const sampleProjects = [
    {
      id: "p1",
      title: "Hệ thống Identity Service & Calendar TCIP",
      description: "Xây dựng dịch vụ định danh người dùng và lịch công tác tích hợp RabbitMQ/Redis",
      status: "Đang thực hiện",
      deadline: "30/10/2025",
      members: 5,
      progress: 75,
    },
    {
      id: "p2",
      title: "Chuyển đổi số doanh nghiệp ERP",
      description: "Triển khai phần mềm quản trị nguồn lực doanh nghiệp cho đối tác chiến lược",
      status: "Kế hoạch",
      deadline: "15/11/2025",
      members: 8,
      progress: 30,
    },
  ];

  return (
    <div className="w-full bg-white rounded-xl border border-slate-200 p-6 shadow-2xs">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-slate-100">
        <div>
          <h1 className="text-xl font-bold text-slate-800 flex items-center gap-2">
            <FolderKanban className="size-5 text-[#0E1E4D]" />
            <span>Quản lý Dự án</span>
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Tổng quan tiến độ các dự án đang triển khai tại TCIP
          </p>
        </div>

        <Button className="h-9 px-4 text-xs font-semibold bg-[#0E1E4D] hover:bg-[#162d6f] text-white rounded-lg shadow-xs flex items-center gap-1.5 cursor-pointer">
          <Plus className="size-4" />
          <span>Thêm dự án mới</span>
        </Button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-6">
        {sampleProjects.map((p) => (
          <div
            key={p.id}
            className="p-5 rounded-xl border border-slate-200 bg-slate-50/40 hover:bg-white hover:border-blue-300 hover:shadow-md transition-all group"
          >
            <div className="flex items-start justify-between gap-2">
              <h3 className="text-sm font-bold text-slate-800 group-hover:text-blue-600 transition-colors">
                {p.title}
              </h3>
              <Badge variant="default">{p.status}</Badge>
            </div>
            <p className="text-xs text-slate-500 mt-2 line-clamp-2">
              {p.description}
            </p>

            <div className="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500">
              <div className="flex items-center gap-1.5">
                <Clock className="size-3.5 text-slate-400" />
                <span>Hạn chót: {p.deadline}</span>
              </div>
              <div className="flex items-center gap-1.5">
                <Users className="size-3.5 text-slate-400" />
                <span>{p.members} thành viên</span>
              </div>
            </div>

            <div className="mt-3">
              <div className="flex justify-between text-[11px] text-slate-500 mb-1">
                <span>Tiến độ</span>
                <span className="font-semibold">{p.progress}%</span>
              </div>
              <div className="w-full bg-slate-200 h-1.5 rounded-full overflow-hidden">
                <div
                  className="bg-blue-600 h-full rounded-full transition-all"
                  style={{ width: `${p.progress}%` }}
                />
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
