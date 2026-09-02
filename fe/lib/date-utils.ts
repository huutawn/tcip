/**
 * Format a Date object to "18 tháng 08 2026" style
 */
export function formatVietnameseFullDate(date: Date): string {
  const day = String(date.getDate()).padStart(2, "0");
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const year = date.getFullYear();
  return `${day} tháng ${month} ${year}`;
}

/**
 * Format month label for mini calendar: "Tháng 8"
 */
export function formatMonthYearHeader(date: Date): string {
  const month = date.getMonth() + 1;
  const year = date.getFullYear();
  return `Tháng ${month} - ${year}`;
}

/**
 * Format Date to "YYYY-MM-DD" in LOCAL timezone (never shifts day due to UTC)
 */
export function formatLocalDateToYMD(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

/**
 * Format time to HH:mm (e.g. 08:00, 14:30) in local timezone
 */
export function formatTime(date: Date | string): string {
  const d = typeof date === "string" ? new Date(date) : date;
  if (isNaN(d.getTime())) return "--:--";
  const hours = String(d.getHours()).padStart(2, "0");
  const minutes = String(d.getMinutes()).padStart(2, "0");
  return `${hours}:${minutes}`;
}

/**
 * Get days in month matrix for calendar grid (including leading/trailing days)
 */
export interface CalendarDay {
  date: Date;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
}

export function getMonthMatrix(currentMonthDate: Date, selectedDate: Date): CalendarDay[] {
  const year = currentMonthDate.getFullYear();
  const month = currentMonthDate.getMonth();

  const firstDayOfMonth = new Date(year, month, 1, 12, 0, 0);
  const lastDayOfMonth = new Date(year, month + 1, 0, 12, 0, 0);

  const startDayOfWeek = firstDayOfMonth.getDay(); // 0 is Sunday
  const daysInMonth = lastDayOfMonth.getDate();

  const days: CalendarDay[] = [];
  const today = new Date();

  // Previous month trailing days
  const prevMonthLastDay = new Date(year, month, 0, 12, 0, 0).getDate();
  for (let i = startDayOfWeek - 1; i >= 0; i--) {
    const d = new Date(year, month - 1, prevMonthLastDay - i, 12, 0, 0);
    days.push({
      date: d,
      dayNumber: prevMonthLastDay - i,
      isCurrentMonth: false,
      isToday: isSameDay(d, today),
      isSelected: isSameDay(d, selectedDate),
    });
  }

  // Current month days
  for (let i = 1; i <= daysInMonth; i++) {
    const d = new Date(year, month, i, 12, 0, 0);
    days.push({
      date: d,
      dayNumber: i,
      isCurrentMonth: true,
      isToday: isSameDay(d, today),
      isSelected: isSameDay(d, selectedDate),
    });
  }

  // Next month leading days to complete 35 or 42 grid cells
  const remaining = 35 - days.length >= 0 ? 35 - days.length : 42 - days.length;
  for (let i = 1; i <= remaining; i++) {
    const d = new Date(year, month + 1, i, 12, 0, 0);
    days.push({
      date: d,
      dayNumber: i,
      isCurrentMonth: false,
      isToday: isSameDay(d, today),
      isSelected: isSameDay(d, selectedDate),
    });
  }

  return days;
}

export function isSameDay(d1: Date, d2: Date): boolean {
  return (
    d1.getFullYear() === d2.getFullYear() &&
    d1.getMonth() === d2.getMonth() &&
    d1.getDate() === d2.getDate()
  );
}

export const VIETNAMESE_WEEKDAYS = ["CN", "T2", "T3", "T4", "T5", "T6", "T7"];
export const ENGLISH_WEEKDAY_KEYS = [
  "Sunday",
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
];
