"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import {
  CalendarEvent,
  CalendarEventMember,
  CreateEventRequest,
  NotificationResponse,
} from "../types/calendar.types";
import { calendarService } from "../services/calendar.service";
import { isSameDay } from "@/lib/date-utils";
import { useAuth } from "@/features/auth/context/auth-context";
import { STORAGE_KEYS } from "@/lib/constants";

export function useCalendar() {
  const { isAuthenticated, isLoading: isAuthLoading } = useAuth();

  const [selectedDate, setSelectedDateState] = useState(() => new Date());
  const [events, setEvents] = useState<CalendarEvent[]>([]);
  const [notifications, setNotifications] = useState<NotificationResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [checkedEventIds, setCheckedEventIds] = useState<
    Record<string, boolean>
  >({});

  /**
   * Chỉ chịu trách nhiệm fetch + update result.
   *
   * Quan trọng:
   * Không setIsLoading(true) ở đây vì function này được gọi từ useEffect.
   */
  const fetchData = useCallback(async () => {
    const [fetchedEvents, fetchedNotifs] = await Promise.all([
      calendarService.getEventsByDay(selectedDate).catch((error) => {
        console.warn("Failed to fetch calendar events:", error);
        return [] as CalendarEvent[];
      }),

      calendarService.getNotifications().catch((error) => {
        console.warn("Failed to fetch notifications:", error);
        return [] as NotificationResponse[];
      }),
    ]);

    return {
      events: fetchedEvents,
      notifications: fetchedNotifs,
    };
  }, [selectedDate]);

  useEffect(() => {
    if (isAuthLoading) {
      return;
    }

    const hasToken = Boolean(
      localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN),
    );

    if (!isAuthenticated && !hasToken) {
      return;
    }

    let cancelled = false;

    const load = async () => {
      try {
        const data = await fetchData();

        if (cancelled) {
          return;
        }

        setEvents(data.events);
        setNotifications(data.notifications);

        setCheckedEventIds(
          Object.fromEntries(
            data.events.map((event) => [event.id, true]),
          ),
        );
      } catch (error) {
        if (!cancelled) {
          console.error("Error loading calendar:", error);
          setEvents([]);
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [isAuthenticated, isAuthLoading, fetchData]);

  /**
   * User đổi ngày -> đây là event handler,
   * nên setLoading(true) ở đây hoàn toàn hợp lý.
   */
  const setSelectedDate = useCallback((date: Date) => {
    setIsLoading(true);
    setSelectedDateState(date);
  }, []);

  /**
   * Manual refresh cũng là event/action,
   * nên có thể setLoading(true) trước request.
   */
  const refreshEvents = useCallback(async () => {
    setIsLoading(true);

    try {
      const data = await fetchData();

      setEvents(data.events);
      setNotifications(data.notifications);

      setCheckedEventIds(
        Object.fromEntries(
          data.events.map((event) => [event.id, true]),
        ),
      );
    } finally {
      setIsLoading(false);
    }
  }, [fetchData]);

  const toggleEventCheck = (id: string) => {
    setCheckedEventIds((prev) => ({
      ...prev,
      [id]: !prev[id],
    }));
  };

  const handleCreateEvent = async (request: CreateEventRequest) => {
    const newEvent = await calendarService.createEvent(request);

    setEvents((prev) => [...prev, newEvent]);

    setCheckedEventIds((prev) => ({
      ...prev,
      [newEvent.id]: true,
    }));

    setIsAddModalOpen(false);

    return newEvent;
  };

  const handleCancelEvent = async (eventId: string) => {
    await calendarService.cancelEvent(eventId);

    setEvents((prev) =>
      prev.filter((event) => event.id !== eventId),
    );

    setCheckedEventIds((prev) => {
      const next = { ...prev };
      delete next[eventId];
      return next;
    });
  };

  const handleCancelReminder = async (eventId: string, reminderId: string) => {
    await calendarService.cancelReminder(eventId, reminderId);
    setEvents((previous) => previous.map((event) =>
      event.id === eventId
        ? {
            ...event,
            reminders: event.reminders.filter((reminder) => reminder.id !== reminderId),
          }
        : event,
    ));
  };

  const handleAddParticipant = async (eventId: string, userId: string) => {
    const participant = await calendarService.addParticipant(eventId, userId);
    setEvents((previous) => previous.map((event) =>
      event.id === eventId
        ? {
            ...event,
            attendees: event.attendees.some((attendee) => attendee.id === participant.id)
              ? event.attendees
              : [...event.attendees, participant].sort((left, right) =>
                  left.displayName.localeCompare(right.displayName),
                ),
          }
        : event,
    ));
    return participant;
  };

  const eventsForSelectedDate = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();

    return events.filter((event) => {
      const matchesDate = isSameDay(
        new Date(event.startAt),
        selectedDate,
      );

      const matchesSearch =
        !query ||
        event.title.toLowerCase().includes(query) ||
        event.description?.toLowerCase().includes(query);

      return matchesDate && matchesSearch;
    });
  }, [events, selectedDate, searchQuery]);

  /**
   * Khi logout thì không cần effect chạy:
   *
   * setEvents([])
   * setNotifications([])
   *
   * Chỉ cần không expose stale data ra UI.
   */
  const canAccessCalendar =
    !isAuthLoading &&
    (isAuthenticated ||
      (typeof window !== "undefined" &&
        Boolean(localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN))));

  return {
    selectedDate,
    setSelectedDate,

    events: canAccessCalendar ? events : [],
    eventsForSelectedDate: canAccessCalendar
      ? eventsForSelectedDate
      : [],

    notifications: canAccessCalendar
      ? notifications
      : [],

    isLoading:
      isAuthLoading || (canAccessCalendar && isLoading),

    searchQuery,
    setSearchQuery,

    isAddModalOpen,
    setIsAddModalOpen,

    checkedEventIds,
    toggleEventCheck,

    handleCreateEvent,
    handleCancelEvent,
    handleCancelReminder,
    handleAddParticipant,

    refreshEvents,
  };
}
