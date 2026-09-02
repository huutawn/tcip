"use client";

import React, { useState } from "react";
import {
  Calendar as CalendarIcon,
  Search,
  Plus,
  Trash2,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { formatVietnameseFullDate, formatTime } from "@/lib/date-utils";
import {
  CalendarEvent,
  CalendarEventMember,
} from "@/features/calendar/types/calendar.types";
import { EventDetailsDialog } from "./event-details-dialog";

interface DayCalendarViewProps {
  selectedDate: Date;
  events: CalendarEvent[];
  onOpenAddModal: () => void;
  onDeleteEvent: (id: string) => Promise<void>;
  onCancelReminder: (eventId: string, reminderId: string) => Promise<void>;
  onAddParticipant: (eventId: string, userId: string) => Promise<CalendarEventMember>;
}

const HOURS = [
  "00:00",
  "07:00",
  "08:00",
  "09:00",
  "10:00",
  "11:00",
  "12:00",
  "13:00",
  "14:00",
  "15:00",
  "16:00",
  "17:00",
  "18:00",
  "19:00",
  "20:00",
  "21:00",
  "22:00",
];

export function DayCalendarView({
  selectedDate,
  events,
  onOpenAddModal,
  onDeleteEvent,
  onCancelReminder,
  onAddParticipant,
}: DayCalendarViewProps) {
  const [activeEventDetails, setActiveEventDetails] = useState<CalendarEvent | null>(null);

  // Group events by approximate start hour
  const getEventsForHour = (hourStr: string) => {
    const targetHour = parseInt(hourStr.split(":")[0], 10);
    return events.filter((e) => {
      const d = new Date(e.startAt);
      return d.getHours() === targetHour;
    });
  };

  return (
    <div className="flex-1 bg-white rounded-lg border border-slate-100 shadow-2xs flex flex-col overflow-hidden select-none">
      {/* Top Header Controls matching calendar.png */}
      <div className="flex items-center justify-between px-5 py-3.5 border-b border-slate-100">
        {/* Date Display */}
        <div className="flex items-center gap-2.5">
          <div className="size-6 rounded-md bg-rose-50 flex items-center justify-center text-rose-600 border border-rose-200">
            <CalendarIcon className="size-3.5" />
          </div>
          <h2 className="text-base font-bold text-slate-800 tracking-tight">
            {formatVietnameseFullDate(selectedDate)}
          </h2>
        </div>

        {/* Right Action Tools */}
        <div className="flex items-center gap-2">
          <button
            type="button"
            className="flex size-8 items-center justify-center rounded-md border border-slate-200 text-slate-500 hover:bg-slate-50 hover:text-slate-700 transition-colors"
            title="Tìm kiếm"
          >
            <Search className="size-4" />
          </button>

          <Button
            onClick={onOpenAddModal}
            className="h-8 px-3 text-xs font-semibold bg-[#0284C7] hover:bg-[#0369A1] text-white rounded-md shadow-xs flex items-center gap-1.5 cursor-pointer"
          >
            <Plus className="size-3.5 stroke-[2.5]" />
            <span>Thêm sự kiện</span>
          </Button>
        </div>
      </div>

      {/* Timeline Schedule Body */}
      <div className="flex-1 overflow-y-auto min-h-[600px] p-2 sm:p-4">
        <div className="flex flex-col divide-y divide-slate-100/80">
          {HOURS.map((hour) => {
            const hourEvents = getEventsForHour(hour);
            const isNoonBlock = hour === "12:00" || hour === "13:00";

            return (
              <div
                key={hour}
                className="flex min-h-[58px] relative group hover:bg-slate-50/40 transition-colors"
              >
                {/* Time Label on Left */}
                <div className="w-16 sm:w-20 pt-2 shrink-0 text-xs font-medium text-slate-400 font-mono">
                  {hour}
                </div>

                {/* Event Area / Slot */}
                <div className="flex-1 pl-2 sm:pl-4 pr-2 py-1 flex flex-col justify-center gap-1.5 relative border-l border-slate-100">
                  {/* Special Noon Highlight block matching calendar.png 12:00-14:00 lavender */}
                  {hour === "12:00" && (
                    <div className="absolute inset-x-2 inset-y-1 rounded-md bg-[#EDE9FE]/70 border border-[#DDD6FE] -z-0 pointer-events-none" />
                  )}
                  {hour === "13:00" && (
                    <div className="absolute inset-x-2 inset-y-1 rounded-md bg-[#EDE9FE]/70 border border-[#DDD6FE] -z-0 pointer-events-none" />
                  )}

                  {hourEvents.map((evt) => {
                    const startTime = formatTime(evt.startAt);
                    const endTime = evt.endAt ? formatTime(evt.endAt) : "";

                    return (
                      <div
                        key={evt.id}
                        onClick={() => setActiveEventDetails(evt)}
                        className={`relative z-10 flex items-center justify-between px-3 py-1.5 rounded-md border text-xs font-medium cursor-pointer transition-all shadow-2xs hover:shadow-xs ${
                          evt.color || "bg-slate-100 text-slate-800 border-slate-200"
                        }`}
                      >
                        {/* Event Left Title */}
                        <div className="flex items-center gap-2 min-w-0">
                          {/* Small icon indicator matching calendar.png */}
                          <span className="size-1.5 rounded-full bg-amber-500 shrink-0" />
                          <span className="truncate font-semibold text-slate-800">
                            {evt.title}
                          </span>
                        </div>

                        {/* Event Right Time Badge & Actions */}
                        <div className="flex items-center gap-3 shrink-0">
                          <span className="text-[11px] font-mono font-medium text-slate-500">
                            {endTime || startTime}
                          </span>

                          <button
                            type="button"
                            onClick={(e) => {
                              e.stopPropagation();
                              setActiveEventDetails(evt);
                            }}
                            className="opacity-0 group-hover:opacity-100 text-slate-400 hover:text-rose-600 transition-opacity p-0.5"
                            title="Mở chi tiết và huỷ sự kiện"
                            aria-label={`Mở chi tiết ${evt.title}`}
                          >
                            <Trash2 className="size-3.5" />
                          </button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
      </div>

      <EventDetailsDialog
        event={activeEventDetails}
        onClose={() => setActiveEventDetails(null)}
        onCancelEvent={onDeleteEvent}
        onCancelReminder={async (eventId, reminderId) => {
          await onCancelReminder(eventId, reminderId);
          setActiveEventDetails((current) => current && current.id === eventId
            ? {
                ...current,
                reminders: current.reminders.map((reminder) =>
                  reminder.id === reminderId
                    ? { ...reminder, status: "Cancelled" }
                    : reminder,
                ),
              }
            : current);
        }}
        onAddParticipant={async (eventId, userId) => {
          const participant = await onAddParticipant(eventId, userId);
          setActiveEventDetails((current) => current && current.id === eventId
            ? {
                ...current,
                attendees: current.attendees.some((attendee) => attendee.id === participant.id)
                  ? current.attendees
                  : [...current.attendees, participant],
              }
            : current);
          return participant;
        }}
      />
    </div>
  );
}
