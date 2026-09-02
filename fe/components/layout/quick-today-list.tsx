"use client";

import React from "react";
import { Calendar as CalendarIcon } from "lucide-react";
import { Checkbox } from "@/components/ui/checkbox";
import { formatTime } from "@/lib/date-utils";
import { CalendarEvent } from "@/features/calendar/types/calendar.types";

interface QuickTodayListProps {
  events: CalendarEvent[];
  checkedEventIds: Record<string, boolean>;
  onToggleEvent: (id: string) => void;
  onSelectEvent?: (event: CalendarEvent) => void;
}

export function QuickTodayList({
  events,
  checkedEventIds,
  onToggleEvent,
  onSelectEvent,
}: QuickTodayListProps) {
  return (
    <div className="w-full bg-white p-3 rounded-lg border border-slate-100 shadow-2xs">
      {/* Header */}
      <div className="flex items-center gap-2 mb-3 px-1">
        <CalendarIcon className="size-4 text-rose-500" />
        <h3 className="text-sm font-bold text-slate-800">Hôm nay</h3>
      </div>

      {/* Events List */}
      <div className="space-y-2 max-h-[340px] overflow-y-auto pr-1">
        {events.length === 0 ? (
          <p className="text-xs text-slate-400 py-3 text-center">
            Không có sự kiện nào
          </p>
        ) : (
          events.map((event) => {
            const isChecked = checkedEventIds[event.id] ?? false;
            const startTimeStr = formatTime(event.startAt);

            return (
              <div
                key={event.id}
                className="flex items-center justify-between gap-2 p-1.5 rounded-md hover:bg-slate-50 transition-colors group"
              >
                <div className="flex items-center gap-2 min-w-0 flex-1">
                  <Checkbox
                    id={`today-evt-${event.id}`}
                    checked={isChecked}
                    onCheckedChange={() => onToggleEvent(event.id)}
                  />
                  <label
                    htmlFor={`today-evt-${event.id}`}
                    onClick={() => onSelectEvent?.(event)}
                    className="text-xs font-medium text-slate-700 hover:text-blue-600 truncate cursor-pointer select-none"
                    title={event.title}
                  >
                    {event.title}
                  </label>
                </div>

                <span className="text-[11px] font-medium text-slate-400 shrink-0 font-mono">
                  {startTimeStr}
                </span>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
