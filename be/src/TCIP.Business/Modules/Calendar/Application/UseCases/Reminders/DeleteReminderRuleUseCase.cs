using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Reminders;

public interface IDeleteReminderRuleUseCase
{
    Task<long> DeleteReminderRuleAsync(
        Guid eventId,
        Guid ruleId,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class DeleteReminderRuleUseCase(
    IEventRepository eventRepository,
    IReminderSchedulePlanner reminderSchedulePlanner,
    TimeProvider timeProvider) : IDeleteReminderRuleUseCase
{
    public async Task<long> DeleteReminderRuleAsync(
        Guid eventId,
        Guid ruleId,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var rawEvent = await eventRepository.GetEventForUpdateAsync(eventId, cancellationToken);
        var calendarEvent = EventAccessValidator.ValidateOwnerAndVersion(
            rawEvent,
            eventId,
            actorUserId,
            expectedVersion,
            "Only the event creator can manage reminder rules.");

        var rule = calendarEvent.ReminderRules.FirstOrDefault(r => r.Id == ruleId)
            ?? throw new NotFoundException($"Reminder rule with ID '{ruleId}' not found on event '{eventId}'.");

        if (rule.Status == ReminderRuleStatus.Cancelled)
        {
            return calendarEvent.Version;
        }

        var now = timeProvider.GetUtcNow();
        rule.Status = ReminderRuleStatus.Cancelled;
        rule.UpdatedAtUtc = now;
        if (rule.Schedule is not null)
        {
            rule.Schedule.Status = ReminderScheduleStatus.Cancelled;
            rule.Schedule.EventVersion = calendarEvent.Version + 1;
            rule.Schedule.UpdatedAtUtc = now;
        }

        calendarEvent.Version++;
        calendarEvent.UpdatedAtUtc = now;

        foreach (var otherRule in calendarEvent.ReminderRules.Where(r => r.Id != ruleId))
        {
            if (otherRule.Schedule is not null)
            {
                reminderSchedulePlanner.UpdateScheduleVersion(otherRule.Schedule, calendarEvent.Version, now);
            }
        }

        await eventRepository.SaveChangesAsync(cancellationToken);
        return calendarEvent.Version;
    }
}
