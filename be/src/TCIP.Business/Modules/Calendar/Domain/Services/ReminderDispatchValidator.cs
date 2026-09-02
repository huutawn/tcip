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
        DateTimeOffset scheduledFireAtUtc,
        int repeatIndex);
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
        DateTimeOffset scheduledFireAtUtc,
        int repeatIndex)
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

        if (repeatIndex < 0 || repeatIndex > rule.RepeatCount)
        {
            return DispatchValidationResult.Drop($"RepeatIndex '{repeatIndex}' is outside rule bounds (RepeatCount={rule.RepeatCount}).");
        }

        var occurrence = recurrenceEngine.ResolveOriginalOccurrence(calendarEvent, originalStartAtUtc);
        if (occurrence is null)
        {
            return DispatchValidationResult.Drop($"Occurrence at '{originalStartAtUtc}' is cancelled or does not belong to the series.");
        }

        var initialFire = occurrence.StartAtUtc.AddMinutes(-rule.RemindBeforeMinutes);
        var expectedFire = initialFire;
        if (repeatIndex > 0 && rule.RepeatEveryMinutes.HasValue)
        {
            expectedFire = initialFire.AddMinutes(repeatIndex * rule.RepeatEveryMinutes.Value);
        }

        if (occurrence.StartAtUtc != effectiveStartAtUtc || expectedFire != scheduledFireAtUtc)
        {
            return DispatchValidationResult.Drop($"Timing changed: expected fire '{expectedFire}', received '{scheduledFireAtUtc}'; expected start '{occurrence.StartAtUtc}', received '{effectiveStartAtUtc}'.");
        }

        return DispatchValidationResult.Valid(occurrence);
    }
}
