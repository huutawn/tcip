"use client";

import { useEffect, useState } from "react";
import { Bell, Clock, Search, UserPlus, Users, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { formatTime } from "@/lib/date-utils";
import {
  CalendarEvent,
  CalendarEventMember,
} from "@/features/calendar/types/calendar.types";
import { calendarService } from "@/features/calendar/services/calendar.service";

interface EventDetailsDialogProps {
  event: CalendarEvent | null;
  onClose: () => void;
  onCancelEvent: (eventId: string) => Promise<void>;
  onCancelReminder: (eventId: string, reminderId: string) => Promise<void>;
  onAddParticipant: (
    eventId: string,
    userId: string,
  ) => Promise<CalendarEventMember>;
}

function describeReminder(reminder: CalendarEvent["reminders"][number]) {
  const before = reminder.remindBeforeMinutes === 0
    ? "Đúng giờ"
    : `Trước ${reminder.remindBeforeMinutes} phút`;
  return reminder.repeatEveryMinutes
    ? `${before}, lặp mỗi ${reminder.repeatEveryMinutes} phút`
    : before;
}

export function EventDetailsDialog({
  event,
  onClose,
  onCancelEvent,
  onCancelReminder,
  onAddParticipant,
}: EventDetailsDialogProps) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<CalendarEventMember[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [pendingReminderId, setPendingReminderId] = useState<string | null>(null);
  const [isCancellingEvent, setIsCancellingEvent] = useState(false);
  const [addingUserId, setAddingUserId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    setQuery("");
    setResults([]);
    setErrorMessage(null);
  }, [event?.id]);

  useEffect(() => {
    if (!event || query.trim().length < 2) {
      setResults([]);
      setIsSearching(false);
      return;
    }

    const controller = new AbortController();
    const timeout = window.setTimeout(() => {
      setIsSearching(true);
      calendarService.searchParticipants(event.id, query.trim(), controller.signal)
        .then((members) => {
          if (!controller.signal.aborted) {
            setResults(members);
          }
        })
        .catch((error: { message?: string }) => {
          if (!controller.signal.aborted) {
            setErrorMessage(error.message ?? "Không thể tìm thành viên.");
          }
        })
        .finally(() => {
          if (!controller.signal.aborted) {
            setIsSearching(false);
          }
        });
    }, 250);

    return () => {
      controller.abort();
      window.clearTimeout(timeout);
    };
  }, [event, query]);

  if (!event) {
    return null;
  }

  const showError = (error: unknown, fallback: string) => {
    setErrorMessage(
      error && typeof error === "object" && "message" in error
        ? String(error.message)
        : fallback,
    );
  };

  const handleCancelEvent = async () => {
    if (!window.confirm(`Bạn có chắc muốn huỷ "${event.title}"?`)) {
      return;
    }

    setIsCancellingEvent(true);
    setErrorMessage(null);
    try {
      await onCancelEvent(event.id);
      onClose();
    } catch (error) {
      showError(error, "Không thể huỷ sự kiện.");
    } finally {
      setIsCancellingEvent(false);
    }
  };

  const handleCancelReminder = async (reminderId: string) => {
    setPendingReminderId(reminderId);
    setErrorMessage(null);
    try {
      await onCancelReminder(event.id, reminderId);
    } catch (error) {
      showError(error, "Không thể huỷ nhắc nhở.");
    } finally {
      setPendingReminderId(null);
    }
  };

  const handleAddParticipant = async (userId: string) => {
    setAddingUserId(userId);
    setErrorMessage(null);
    try {
      await onAddParticipant(event.id, userId);
      setQuery("");
      setResults([]);
    } catch (error) {
      showError(error, "Không thể thêm thành viên.");
    } finally {
      setAddingUserId(null);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4" role="presentation">
      <button
        type="button"
        aria-label="Đóng chi tiết sự kiện"
        className="fixed inset-0 cursor-default bg-slate-900/30 backdrop-blur-2xs"
        onClick={onClose}
      />
      <section
        aria-labelledby="event-details-title"
        aria-modal="true"
        role="dialog"
        className="relative z-10 max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-xl border border-slate-100 bg-white p-5 shadow-2xl"
      >
        <div className="flex items-start justify-between gap-4">
          <div>
            <h3 id="event-details-title" className="text-sm font-bold text-slate-900">
              {event.title}
            </h3>
            {event.description && <p className="mt-1 text-xs text-slate-600">{event.description}</p>}
          </div>
          <button
            type="button"
            aria-label="Đóng"
            className="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
            onClick={onClose}
          >
            <X className="size-4" />
          </button>
        </div>

        {errorMessage && (
          <p role="alert" className="mt-3 rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-xs text-rose-700">
            {errorMessage}
          </p>
        )}

        <div className="mt-4 space-y-2 border-t border-slate-100 pt-3 text-xs text-slate-600">
          <div className="flex items-center gap-2"><Clock className="size-3.5" />
            <span>{formatTime(event.startAt)}{event.endAt && ` - ${formatTime(event.endAt)}`}</span>
          </div>
          <div className="flex items-center gap-2"><Users className="size-3.5" />
            <span>{event.attendees.length} người tham gia trực tiếp</span>
          </div>
        </div>

        <div className="mt-5 border-t border-slate-100 pt-4">
          <div className="flex items-center gap-2 text-xs font-semibold text-slate-700">
            <Bell className="size-3.5" /> Nhắc nhở
          </div>
          <ul className="mt-2 space-y-2" aria-label="Danh sách nhắc nhở">
            {event.reminders.length === 0 && <li className="text-xs text-slate-500">Chưa có nhắc nhở.</li>}
            {event.reminders.map((reminder) => (
              <li key={reminder.id} className="flex items-center justify-between gap-3 rounded-md border border-slate-200 px-3 py-2 text-xs">
                <span className="text-slate-700">{describeReminder(reminder)}</span>
                {reminder.status === "Active" ? (
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    disabled={pendingReminderId === reminder.id}
                    onClick={() => void handleCancelReminder(reminder.id)}
                  >
                    {pendingReminderId === reminder.id ? "Đang huỷ..." : "Huỷ nhắc"}
                  </Button>
                ) : <span className="text-slate-500">{reminder.status === "Cancelled" ? "Đã huỷ" : "Đã xong"}</span>}
              </li>
            ))}
          </ul>
        </div>

        <div className="mt-5 border-t border-slate-100 pt-4">
          <label htmlFor="participant-search" className="flex items-center gap-2 text-xs font-semibold text-slate-700">
            <UserPlus className="size-3.5" /> Thêm thành viên
          </label>
          <div className="relative mt-2">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-3.5 -translate-y-1/2 text-slate-400" />
            <input
              id="participant-search"
              type="search"
              value={query}
              onChange={(input) => setQuery(input.target.value)}
              placeholder="Tìm theo tên hoặc email (ít nhất 2 ký tự)"
              className="h-9 w-full rounded-md border border-slate-200 pl-9 pr-3 text-xs outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-100"
            />
          </div>
          {isSearching && <p className="mt-2 text-xs text-slate-500">Đang tìm...</p>}
          {query.trim().length >= 2 && !isSearching && results.length === 0 && (
            <p className="mt-2 text-xs text-slate-500">Không tìm thấy thành viên phù hợp.</p>
          )}
          {results.length > 0 && (
            <ul className="mt-2 divide-y divide-slate-100 rounded-md border border-slate-200" aria-label="Kết quả tìm thành viên">
              {results.map((member) => (
                <li key={member.id} className="flex items-center justify-between gap-3 px-3 py-2">
                  <div className="min-w-0 text-xs"><p className="truncate font-medium text-slate-700">{member.displayName}</p><p className="truncate text-slate-500">{member.email}</p></div>
                  <Button type="button" size="sm" disabled={addingUserId === member.id} onClick={() => void handleAddParticipant(member.id)}>
                    {addingUserId === member.id ? "Đang thêm..." : "Thêm"}
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="mt-5 flex justify-between gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="destructive" size="sm" disabled={isCancellingEvent} onClick={() => void handleCancelEvent()}>
            {isCancellingEvent ? "Đang huỷ..." : "Huỷ sự kiện"}
          </Button>
          <Button type="button" variant="secondary" size="sm" onClick={onClose}>Đóng</Button>
        </div>
      </section>
    </div>
  );
}
