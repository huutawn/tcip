import { apiClient } from "@/lib/api-client";
import {
  CalendarEvent,
  CalendarEventMember,
  CreateEventRequest,
  NotificationResponse,
} from "../types/calendar.types";
import { formatLocalDateToYMD } from "@/lib/date-utils";

export const EVENT_COLOR_PALETTES = [
  "bg-[#F3F4F6] text-gray-800 border-gray-200 hover:bg-gray-100",
  "bg-[#FFF9DB] text-amber-900 border-amber-300 hover:bg-[#FFF3BF]",
  "bg-[#E0E7FF] text-indigo-900 border-indigo-200 hover:bg-[#C7D2FE]",
  "bg-[#FFE4E6] text-rose-900 border-rose-200 hover:bg-[#FECDD3]",
  "bg-[#FEF3C7] text-amber-900 border-amber-200 hover:bg-[#FDE68A]",
  "bg-[#D1FAE5] text-emerald-900 border-emerald-200 hover:bg-[#A7F3D0]",
  "bg-[#FFEDD5] text-orange-900 border-orange-200 hover:bg-[#FED7AA]",
  "bg-[#E8F1FD] text-blue-900 border-blue-200 hover:bg-[#DBEAFE]",
];

export function getEventColor(index: number): string {
  return EVENT_COLOR_PALETTES[index % EVENT_COLOR_PALETTES.length];
}

export const calendarService = {
  async getEventsByDay(day?: Date | string): Promise<CalendarEvent[]> {
    let url = "/api/calendar/events/by-day";
    if (day) {
      const dateStr =
        typeof day === "string"
          ? (day.includes("T") ? day : `${day}T00:00:00Z`)
          : `${formatLocalDateToYMD(day)}T00:00:00Z`;
      url += `?day=${encodeURIComponent(dateStr)}`;
    }
    const data = await apiClient.get<CalendarEvent[]>(url);
    if (!Array.isArray(data)) return [];
    
    // Assign consistent badge colors for UI presentation
    return data.map((evt, idx) => ({
      ...evt,
      color: evt.color || getEventColor(idx),
    }));
  },

  async getEvents(day?: Date | string): Promise<CalendarEvent[]> {
    return this.getEventsByDay(day);
  },

  async getAllEvents(): Promise<CalendarEvent[]> {
    const data = await apiClient.get<CalendarEvent[]>("/api/calendar/events");
    if (!Array.isArray(data)) return [];
    return data.map((evt, idx) => ({
      ...evt,
      color: evt.color || getEventColor(idx),
    }));
  },

  async createEvent(request: CreateEventRequest): Promise<CalendarEvent> {
    const created = await apiClient.post<CalendarEvent>(
      "/api/calendar/events",
      request
    );
    return {
      ...created,
      color: created.color || getEventColor(Math.floor(Math.random() * EVENT_COLOR_PALETTES.length)),
    };
  },

  async cancelEvent(eventId: string): Promise<void> {
    await apiClient.delete(`/api/calendar/events/${eventId}`);
  },

  async cancelReminder(eventId: string, reminderId: string): Promise<void> {
    await apiClient.delete(`/api/calendar/events/${eventId}/reminders/${reminderId}`);
  },

  async searchUsers(
    query: string,
    signal?: AbortSignal,
  ): Promise<CalendarEventMember[]> {
    const data = await apiClient.get<CalendarEventMember[]>(
      `/api/calendar/participant-search?query=${encodeURIComponent(query)}`,
      { signal },
    );
    return Array.isArray(data) ? data : [];
  },

  async searchParticipants(
    eventId: string,
    query: string,
    signal?: AbortSignal,
  ): Promise<CalendarEventMember[]> {
    const data = await apiClient.get<CalendarEventMember[]>(
      `/api/calendar/events/${eventId}/participant-search?query=${encodeURIComponent(query)}`,
      { signal },
    );
    return Array.isArray(data) ? data : [];
  },

  async addParticipant(
    eventId: string,
    userId: string,
  ): Promise<CalendarEventMember> {
    return apiClient.post<CalendarEventMember>(
      `/api/calendar/events/${eventId}/participants`,
      { userId },
    );
  },

  async getNotifications(): Promise<NotificationResponse[]> {
    const data = await apiClient.get<NotificationResponse[]>(
      "/api/calendar/notifications"
    );
    return Array.isArray(data) ? data : [];
  },

  async markNotificationRead(notificationId: string): Promise<void> {
    await apiClient.put(`/api/calendar/notifications/${notificationId}/read`);
  },
};
