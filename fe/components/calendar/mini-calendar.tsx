"use client";

import React, { useState, useEffect } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import {
  formatMonthYearHeader,
  getMonthMatrix,
  VIETNAMESE_WEEKDAYS,
} from "@/lib/date-utils";
import { cn } from "@/lib/utils";

interface MiniCalendarProps {
  selectedDate: Date;
  onSelectDate: (date: Date) => void;
}

export function MiniCalendar({
  selectedDate,
  onSelectDate,
}: MiniCalendarProps) {
  const [currentMonthDate, setCurrentMonthDate] = useState<Date>(
    new Date(selectedDate.getFullYear(), selectedDate.getMonth(), 1, 12, 0, 0)
  );

  useEffect(() => {
    setCurrentMonthDate(
      new Date(selectedDate.getFullYear(), selectedDate.getMonth(), 1, 12, 0, 0)
    );
  }, [selectedDate]);

  const daysMatrix = getMonthMatrix(currentMonthDate, selectedDate);

  const handlePrevMonth = () => {
    setCurrentMonthDate(
      new Date(
        currentMonthDate.getFullYear(),
        currentMonthDate.getMonth() - 1,
        1,
        12,
        0,
        0
      )
    );
  };

  const handleNextMonth = () => {
    setCurrentMonthDate(
      new Date(
        currentMonthDate.getFullYear(),
        currentMonthDate.getMonth() + 1,
        1,
        12,
        0,
        0
      )
    );
  };

  return (
    <div className="w-full bg-white p-3 rounded-lg border border-slate-100 shadow-2xs">
      {/* Month Header */}
      <div className="flex items-center justify-between mb-3 px-1">
        <h3 className="text-sm font-bold text-slate-800">
          {formatMonthYearHeader(currentMonthDate)}
        </h3>
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={handlePrevMonth}
            className="p-1 rounded-md text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-colors cursor-pointer"
            title="Tháng trước"
          >
            <ChevronLeft className="size-3.5" />
          </button>
          <button
            type="button"
            onClick={handleNextMonth}
            className="p-1 rounded-md text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-colors cursor-pointer"
            title="Tháng sau"
          >
            <ChevronRight className="size-3.5" />
          </button>
        </div>
      </div>

      {/* Weekday Labels CN, T2, T3, T4, T5, T6, T7 */}
      <div className="grid grid-cols-7 text-center mb-1">
        {VIETNAMESE_WEEKDAYS.map((day) => (
          <span
            key={day}
            className="text-[11px] font-semibold text-slate-600 py-0.5"
          >
            {day}
          </span>
        ))}
      </div>

      {/* Days Grid */}
      <div className="grid grid-cols-7 gap-y-1 text-center">
        {daysMatrix.map((item, idx) => {
          const isSelected = item.isSelected;
          const isCurrentMonth = item.isCurrentMonth;
          const formattedDay = String(item.dayNumber).padStart(2, "0");

          return (
            <div
              key={idx}
              className="flex items-center justify-center p-0.5"
            >
              <button
                type="button"
                onClick={() => {
                  onSelectDate(item.date);
                  if (!item.isCurrentMonth) {
                    setCurrentMonthDate(
                      new Date(item.date.getFullYear(), item.date.getMonth(), 1, 12, 0, 0)
                    );
                  }
                }}
                className={cn(
                  "size-7 rounded-full text-xs font-medium flex items-center justify-center transition-all duration-150 cursor-pointer",
                  isSelected
                    ? "bg-[#D81B60] text-white font-bold shadow-xs scale-105"
                    : item.isToday
                    ? "border border-rose-400 text-rose-600 font-semibold"
                    : isCurrentMonth
                    ? "text-slate-700 hover:bg-slate-100"
                    : "text-slate-300 hover:bg-slate-50"
                )}
              >
                {formattedDay}
              </button>
            </div>
          );
        })}
      </div>
    </div>
  );
}
