import { apiClient } from "@/lib/api-client";
import { CalendarEvent, CalendarEventMember, CreateEventRequest, NotificationResponse } from "../types/calendar.types";
import { formatLocalDateToYMD } from "@/lib/date-utils";

export const EVENT_COLOR_PALETTES = ["bg-[#F3F4F6] text-gray-800 border-gray-200 hover:bg-gray-100", "bg-[#FFF9DB] text-amber-900 border-amber-300 hover:bg-[#FFF3BF]", "bg-[#E0E7FF] text-indigo-900 border-indigo-200 hover:bg-indigo-100", "bg-[#FFE4E6] text-rose-900 border-rose-200 hover:bg-rose-100", "bg-[#FEF3C7] text-amber-900 border-amber-300 hover:bg-amber-100", "bg-[#D1FAE5] text-emerald-900 border-emerald-200 hover:bg-emerald-100", "bg-[#FFEDD5] text-orange-900 border-orange-200 hover:bg-orange-100", "bg-[#E8F1FD] text-blue-900 border-blue-200 hover:bg-blue-100"];

export function getEventColor(index: number): string { return EVENT_COLOR_PALETTES[index % EVENT_COLOR_PALETTES.length]; }

interface EventOccurrenceResponse { eventId: string; startAt: string; endAt?: string; title: string; description?: string; timeZoneId: string; status: CalendarEvent["status"]; eventVersion: number; }
interface CalendarEventsByDayResponse { items: EventOccurrenceResponse[]; }
interface EventDetailResponse { id: string; startAt: string; endAt?: string; timeZoneId: string; recurrenceRule?: string; status: CalendarEvent["status"]; version: number; translations: Array<{ language: string; title: string; description?: string }>; audiences: Array<{ principalId: string; principalName?: string }>; reminderRules: Array<{ id: string; remindBeforeMinutes: number; repeatEveryMinutes?: number; status: CalendarEvent["reminders"][number]["status"] }>; }
interface PrincipalSearchResponse { items: Array<{ principalId: string; name: string; email?: string }>; }

function mapPrincipal(principal: PrincipalSearchResponse["items"][number]): CalendarEventMember { return { id: principal.principalId, displayName: principal.name, email: principal.email ?? "" }; }
function mapOccurrence(event: EventOccurrenceResponse, index: number): CalendarEvent { return { id: event.eventId, startAt: event.startAt, endAt: event.endAt, timeZoneId: event.timeZoneId, status: event.status, version: event.eventVersion, isRecurring: false, recurringWeekdays: [], title: event.title, description: event.description, reminders: [], attendees: [], color: getEventColor(index) }; }
function mapDetail(event: EventDetailResponse): CalendarEvent {
  const translation = event.translations.find((item) => item.language === "vi") ?? event.translations[0];
  return { id: event.id, startAt: event.startAt, endAt: event.endAt, timeZoneId: event.timeZoneId, status: event.status, version: event.version, isRecurring: Boolean(event.recurrenceRule), recurringWeekdays: [], title: translation?.title ?? "Sự kiện không có tiêu đề", description: translation?.description, reminders: event.reminderRules, attendees: event.audiences.map((audience) => ({ id: audience.principalId, displayName: audience.principalName ?? "Không rõ", email: "" })) };
}
async function getEventDetail(eventId: string): Promise<EventDetailResponse> { return apiClient.get<EventDetailResponse>(`/api/calendar/events/${eventId}`); }

export const calendarService = {
  async getEventsByDay(day?: Date | string): Promise<CalendarEvent[]> {
    let url = "/api/calendar/events/by-day";
    if (day) { const dateStr = typeof day === "string" ? (day.includes("T") ? day : `${day}T00:00:00Z`) : `${formatLocalDateToYMD(day)}T00:00:00Z`; url += `?day=${encodeURIComponent(dateStr)}`; }
    const data = await apiClient.get<CalendarEventsByDayResponse>(url);
    return data.items.map(mapOccurrence);
  },
  async createEvent(request: CreateEventRequest): Promise<CalendarEvent> { return mapDetail(await apiClient.post<EventDetailResponse>("/api/calendar/events", request)); },
  async cancelEvent(eventId: string): Promise<void> { const event = await getEventDetail(eventId); await apiClient.delete(`/api/calendar/events/${eventId}`, { headers: { "If-Match": `"${event.version}"` } }); },
  async cancelReminder(eventId: string, reminderId: string): Promise<void> { const event = await getEventDetail(eventId); await apiClient.delete(`/api/calendar/events/${eventId}/reminder-rules/${reminderId}`, { headers: { "If-Match": `"${event.version}"` } }); },
  async searchUsers(query: string, signal?: AbortSignal): Promise<CalendarEventMember[]> { const data = await apiClient.get<PrincipalSearchResponse>(`/api/rbac/principals?type=User&available=true&search=${encodeURIComponent(query)}`, { signal }); return data.items.map(mapPrincipal); },
  async searchParticipants(_eventId: string, query: string, signal?: AbortSignal): Promise<CalendarEventMember[]> { return this.searchUsers(query, signal); },
  async addParticipant(eventId: string, principalId: string): Promise<CalendarEventMember> {
    const [event, principal] = await Promise.all([getEventDetail(eventId), apiClient.get<PrincipalSearchResponse["items"][number]>(`/api/rbac/principals/${principalId}`)]);
    await apiClient.put(`/api/calendar/events/${eventId}/audiences/${principalId}`, undefined, { headers: { "If-Match": `"${event.version}"` } });
    return mapPrincipal(principal);
  },
  async getNotifications(): Promise<NotificationResponse[]> {
    const data = await apiClient.get<Array<NotificationResponse & { originalStartAt: string }>>("/api/calendar/notifications");
    return data.map(({ originalStartAt, ...notification }) => ({ ...notification, occurrenceStartAt: originalStartAt }));
  },
  async markNotificationRead(notificationId: string): Promise<void> { await apiClient.put(`/api/calendar/notifications/${notificationId}/read`); },
};
