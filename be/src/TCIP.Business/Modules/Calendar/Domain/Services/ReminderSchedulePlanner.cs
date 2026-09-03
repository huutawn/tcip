using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Models;

namespace TCIP.Business.Modules.Calendar.Domain.Services;

public interface IReminderSchedulePlanner
{
    void InitializeOrRebuildSchedule(
        ReminderRule rule,
        Event calendarEvent,
        DateTimeOffset now,
        bool isRebuild = false);

    void UpdateScheduleVersion(
        ReminderSchedule schedule,
        long newEventVersion,
        DateTimeOffset now);

    void ReprojectSchedule(
        ReminderRule rule,
        Event calendarEvent,
        DateTimeOffset now);
}

public sealed class ReminderSchedulePlanner(IRecurrenceEngine recurrenceEngine) : IReminderSchedulePlanner
{
    public void InitializeOrRebuildSchedule(
        ReminderRule rule,
        Event calendarEvent,
        DateTimeOffset now,
        bool isRebuild = false)
    {
        if (calendarEvent.Status != EventStatus.Active || rule.Status != ReminderRuleStatus.Active)
        {
            if (rule.Schedule is not null)
            {
                rule.Schedule.Status = ReminderScheduleStatus.Cancelled;
                rule.Schedule.EventVersion = calendarEvent.Version;
                rule.Schedule.UpdatedAtUtc = now;
            }
            return;
        }

        var searchHorizon = isRebuild
            ? now - TimeSpan.FromMinutes(rule.MaxLatenessMinutes)
            : DateTimeOffset.MinValue;

        var exceptions = calendarEvent.OccurrenceExceptions.ToList();
        var nextOccurrence = recurrenceEngine.GetNextOccurrence(
            calendarEvent.RecurrenceRule,
            calendarEvent.StartAtUtc,
            calendarEvent.EndAtUtc,
            calendarEvent.TimeZoneId,
            searchHorizon,
            exceptions);

        if (nextOccurrence is null)
        {
            if (rule.Schedule is null)
            {
                rule.Schedule = new ReminderSchedule
                {
                    ReminderRuleId = rule.Id,
                    OccurrenceStartAtUtc = calendarEvent.StartAtUtc,
                    NextFireAtUtc = calendarEvent.StartAtUtc.AddMinutes(-rule.RemindBeforeMinutes),
                    EventVersion = calendarEvent.Version,
                    Status = ReminderScheduleStatus.Completed,
                    UpdatedAtUtc = now
                };
            }
            else
            {
                rule.Schedule.Status = ReminderScheduleStatus.Completed;
                rule.Schedule.EventVersion = calendarEvent.Version;
                rule.Schedule.UpdatedAtUtc = now;
            }
            return;
        }

        var effStart = nextOccurrence.StartAtUtc;
        var initialFire = effStart.AddMinutes(-rule.RemindBeforeMinutes);

        if (rule.Schedule is null)
        {
            rule.Schedule = new ReminderSchedule
            {
                ReminderRuleId = rule.Id,
                OccurrenceStartAtUtc = nextOccurrence.OriginalStartAtUtc,
                NextFireAtUtc = initialFire,
                EventVersion = calendarEvent.Version,
                Status = ReminderScheduleStatus.Active,
                UpdatedAtUtc = now
            };
        }
        else
        {
            rule.Schedule.OccurrenceStartAtUtc = nextOccurrence.OriginalStartAtUtc;
            rule.Schedule.NextFireAtUtc = initialFire;
            rule.Schedule.EventVersion = calendarEvent.Version;
            rule.Schedule.Status = ReminderScheduleStatus.Active;
            rule.Schedule.UpdatedAtUtc = now;
        }
    }

    public void UpdateScheduleVersion(
        ReminderSchedule schedule,
        long newEventVersion,
        DateTimeOffset now)
    {
        schedule.EventVersion = newEventVersion;
        schedule.UpdatedAtUtc = now;
    }

    public void ReprojectSchedule(
        ReminderRule rule,
        Event calendarEvent,
        DateTimeOffset now)
    {
        InitializeOrRebuildSchedule(rule, calendarEvent, now, isRebuild: true);
    }
}
