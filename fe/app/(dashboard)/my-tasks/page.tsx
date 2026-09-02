"use client";

import React, { useState } from "react";
import { Briefcase, CheckCircle2, Circle, Clock, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";

export default function MyTasksPage() {
  const [tasks, setTasks] = useState([
    {
      id: "t1",
      title: "Hoàn thiện code header & sidebar theo design",
      dueDate: "Hôm nay, 17:00",
      priority: "Cao",
      completed: true,
    },
    {
      id: "t2",
      title: "Kiểm thử API login/register với Backend .NET Identity",
      dueDate: "Hôm nay, 18:00",
      priority: "Cao",
      completed: true,
    },
    {
      id: "t3",
      title: "Chuẩn bị tài liệu báo cáo Sprint",
      dueDate: "Ngày mai, 12:00",
      priority: "Trung bình",
      completed: false,
    },
    {
      id: "t4",
      title: "Review pull request của nhóm Frontend",
      dueDate: "20/10/2025",
      priority: "Thấp",
      completed: false,
    },
  ]);

  const toggleTask = (id: string) => {
    setTasks((prev) =>
      prev.map((t) => (t.id === id ? { ...t, completed: !t.completed } : t))
    );
  };

  return (
    <div className="w-full bg-white rounded-xl border border-slate-200 p-6 shadow-2xs">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-slate-100">
        <div>
          <h1 className="text-xl font-bold text-slate-800 flex items-center gap-2">
            <Briefcase className="size-5 text-[#0E1E4D]" />
            <span>Công việc của tôi</span>
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Danh sách nhiệm vụ cá nhân cần hoàn thành
          </p>
        </div>

        <Button className="h-9 px-4 text-xs font-semibold bg-[#0E1E4D] hover:bg-[#162d6f] text-white rounded-lg shadow-xs flex items-center gap-1.5 cursor-pointer">
          <Plus className="size-4" />
          <span>Thêm công việc</span>
        </Button>
      </div>

      <div className="divide-y divide-slate-100 mt-4">
        {tasks.map((task) => (
          <div
            key={task.id}
            className="py-3 flex items-center justify-between gap-3 text-xs hover:bg-slate-50/70 px-2 rounded-lg transition-colors cursor-pointer"
            onClick={() => toggleTask(task.id)}
          >
            <div className="flex items-center gap-3">
              <button
                type="button"
                className="text-slate-400 hover:text-blue-600 cursor-pointer"
              >
                {task.completed ? (
                  <CheckCircle2 className="size-4 text-emerald-500" />
                ) : (
                  <Circle className="size-4 text-slate-300" />
                )}
              </button>
              <div>
                <p
                  className={`font-semibold ${
                    task.completed
                      ? "line-through text-slate-400"
                      : "text-slate-800"
                  }`}
                >
                  {task.title}
                </p>
                <div className="flex items-center gap-1.5 text-slate-400 text-[11px] mt-0.5">
                  <Clock className="size-3" />
                  <span>{task.dueDate}</span>
                </div>
              </div>
            </div>

            <Badge
              variant={
                task.priority === "Cao"
                  ? "destructive"
                  : task.priority === "Trung bình"
                  ? "warning"
                  : "secondary"
              }
            >
              {task.priority}
            </Badge>
          </div>
        ))}
      </div>
    </div>
  );
}
