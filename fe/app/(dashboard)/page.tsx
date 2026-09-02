"use client";

import React from "react";
import { MiniCalendar } from "@/components/calendar/mini-calendar";
import { QuickTodayList } from "@/components/layout/quick-today-list";
import { DayCalendarView } from "@/components/calendar/day-calendar-view";
import { AddEventModal } from "@/components/calendar/add-event-modal";
import { useCalendar } from "@/features/calendar/hooks/use-calendar";

export default function CalendarDashboardPage() {
  const {
    selectedDate,
    setSelectedDate,
    eventsForSelectedDate,
    events,
    checkedEventIds,
    toggleEventCheck,
    isAddModalOpen,
    setIsAddModalOpen,
    handleCreateEvent,
    handleCancelEvent,
    handleCancelReminder,
    handleAddParticipant,
    isLoading,
  } = useCalendar();

  return (
    <div className="flex flex-col lg:flex-row gap-4 w-full h-full min-h-[calc(100vh-160px)]">
      {/* Calendar Specific Left Sub-panel: MiniCalendar + Today's Checklist */}
      <div className="w-full lg:w-72 shrink-0 flex flex-col gap-3">
        <MiniCalendar
          selectedDate={selectedDate}
          onSelectDate={setSelectedDate}
        />

        <QuickTodayList
          events={events}
          checkedEventIds={checkedEventIds}
          onToggleEvent={toggleEventCheck}
          onSelectEvent={(evt) => setSelectedDate(new Date(evt.startAt))}
        />
      </div>

      {/* Main Day Timeline Calendar View */}
      <div className="flex-1 min-w-0">
        <DayCalendarView
          selectedDate={selectedDate}
          events={eventsForSelectedDate}
          onOpenAddModal={() => setIsAddModalOpen(true)}
          onDeleteEvent={handleCancelEvent}
          onCancelReminder={handleCancelReminder}
          onAddParticipant={handleAddParticipant}
        />
      </div>

      {/* Add Event Modal */}
      <AddEventModal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        onSave={handleCreateEvent}
        initialDate={selectedDate}
      />
    </div>
  );
}
