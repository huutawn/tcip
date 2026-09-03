using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Models;

namespace TCIP.Business.Modules.Calendar.Domain.Services;

public interface IReminderDispatchValidator
{
    DispatchValidationResult ValidateDispatch(
        Event? calendarEvent,
        ReminderRule? rule,
        Guid eventId,
        Guid reminderRuleId,
        DateTimeOffset originalStartAtUtc,
        DateTimeOffset effectiveStartAtUtc,
        DateTimeOffset scheduledFireAtUtc);
}

public sealed class ReminderDispatchValidator(IRecurrenceEngine recurrenceEngine) : IReminderDispatchValidator
{
    public DispatchValidationResult ValidateDispatch(
        Event? calendarEvent,
        ReminderRule? rule,
        Guid eventId,
        Guid reminderRuleId,
        DateTimeOffset originalStartAtUtc,
        DateTimeOffset effectiveStartAtUtc,
        DateTimeOffset scheduledFireAtUtc)
    {
        if (calendarEvent is null || calendarEvent.Status != EventStatus.Active)
        {
            return DispatchValidationResult.Drop("Event cancelled or deleted.");
        }

        if (calendarEvent.Id != eventId)
        {
            return DispatchValidationResult.Drop("Event ID mismatch.");
        }

        if (rule is null || rule.Status != ReminderRuleStatus.Active || rule.EventId != eventId || rule.Id != reminderRuleId)
        {
            return DispatchValidationResult.Drop("Reminder rule inactive or mismatched.");
        }

        var occurrence = recurrenceEngine.ResolveOriginalOccurrence(calendarEvent, originalStartAtUtc);
        if (occurrence is null)
        {
            return DispatchValidationResult.Drop($"Occurrence at '{originalStartAtUtc}' is cancelled or does not belong to the series.");
        }

        var initialFire = occurrence.StartAtUtc.AddMinutes(-rule.RemindBeforeMinutes);
        if (occurrence.StartAtUtc != effectiveStartAtUtc ||
            scheduledFireAtUtc < initialFire ||
            scheduledFireAtUtc >= occurrence.StartAtUtc)
        {
            return DispatchValidationResult.Drop($"Timing changed: received fire '{scheduledFireAtUtc}', expected a time from '{initialFire}' until '{occurrence.StartAtUtc}'; expected start '{occurrence.StartAtUtc}', received '{effectiveStartAtUtc}'.");
        }

        if (!rule.RepeatEveryMinutes.HasValue && scheduledFireAtUtc != initialFire)
        {
            return DispatchValidationResult.Drop($"Timing changed: expected fire '{initialFire}', received '{scheduledFireAtUtc}'.");
        }

        if (rule.RepeatEveryMinutes.HasValue &&
            (scheduledFireAtUtc - initialFire).Ticks % TimeSpan.FromMinutes(rule.RepeatEveryMinutes.Value).Ticks != 0)
        {
            return DispatchValidationResult.Drop($"Scheduled fire '{scheduledFireAtUtc}' is outside the {rule.RepeatEveryMinutes.Value}-minute repeat cadence.");
        }

        return DispatchValidationResult.Valid(occurrence);
    }
}
