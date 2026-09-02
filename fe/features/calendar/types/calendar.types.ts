export type EventStatus = "Active" | "Cancelled";
export type DayOfWeek =
  | "Sunday"
  | "Monday"
  | "Tuesday"
  | "Wednesday"
  | "Thursday"
  | "Friday"
  | "Saturday";

export interface EventTranslationRequest {
  language: string;
  title: string;
  description?: string;
}

export interface CreateEventRequest {
  startAt: string; // ISO 8601 string
  endAt?: string;
  timeZoneId: string;
  isRecurring: boolean;
  recurringWeekdays: DayOfWeek[];
  recurrenceEndAt?: string;
  translations: EventTranslationRequest[];
  userIds: string[];
  groupIds: string[];
  reminders: ReminderRequest[];
}

export interface ReminderRequest {
  remindBeforeMinutes: number;
  repeatEveryMinutes?: number;
}

export interface EventReminder extends ReminderRequest {
  id: string;
  status: "Active" | "Completed" | "Cancelled";
}

export interface CalendarEventMember {
  id: string;
  displayName: string;
  email: string;
}

export interface CalendarEvent {
  id: string;
  startAt: string;
  endAt?: string;
  timeZoneId: string;
  status: EventStatus;
  isRecurring: boolean;
  recurringWeekdays: DayOfWeek[];
  recurrenceEndAt?: string;
  title: string;
  description?: string;
  reminders: EventReminder[];
  color?: string; // UI accent color
  attendees: CalendarEventMember[];
}

export interface NotificationResponse {
  id: string;
  eventId: string;
  occurrenceStartAt: string;
  title: string;
  description?: string;
  sentAt: string;
  readAt?: string;
}
