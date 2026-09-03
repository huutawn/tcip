using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Reminders;

public interface IUpdateReminderRuleUseCase
{
    Task<(ReminderRuleResponse Response, long NewVersion)> UpdateReminderRuleAsync(
        Guid eventId,
        Guid ruleId,
        UpdateReminderRuleRequest request,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class UpdateReminderRuleUseCase(
    IEventRepository eventRepository,
    IReminderSchedulePlanner reminderSchedulePlanner,
    TimeProvider timeProvider) : IUpdateReminderRuleUseCase
{
    public async Task<(ReminderRuleResponse Response, long NewVersion)> UpdateReminderRuleAsync(
        Guid eventId,
        Guid ruleId,
        UpdateReminderRuleRequest request,
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

        ReminderRuleValidator.Validate(
            request.RemindBeforeMinutes,
            request.RepeatEveryMinutes,
            request.MaxLatenessMinutes);

        var statusUnchanged = !request.Status.HasValue || request.Status.Value == rule.Status;
        var ruleUnchanged = rule.RemindBeforeMinutes == request.RemindBeforeMinutes &&
                            rule.RepeatEveryMinutes == request.RepeatEveryMinutes &&
                            rule.MisfirePolicy == request.MisfirePolicy &&
                            rule.MaxLatenessMinutes == request.MaxLatenessMinutes &&
                            statusUnchanged;

        if (ruleUnchanged)
        {
            return (CalendarResponseMapper.MapRule(rule), calendarEvent.Version);
        }

        var now = timeProvider.GetUtcNow();
        rule.RemindBeforeMinutes = request.RemindBeforeMinutes;
        rule.RepeatEveryMinutes = request.RepeatEveryMinutes;
        rule.MisfirePolicy = request.MisfirePolicy;
        rule.MaxLatenessMinutes = request.MaxLatenessMinutes;
        if (request.Status.HasValue)
        {
            rule.Status = request.Status.Value;
        }
        rule.UpdatedAtUtc = now;

        calendarEvent.Version++;
        calendarEvent.UpdatedAtUtc = now;

        reminderSchedulePlanner.ReprojectSchedule(rule, calendarEvent, now);

        foreach (var otherRule in calendarEvent.ReminderRules.Where(r => r.Id != ruleId))
        {
            if (otherRule.Schedule is not null)
            {
                reminderSchedulePlanner.UpdateScheduleVersion(otherRule.Schedule, calendarEvent.Version, now);
            }
        }

        await eventRepository.SaveChangesAsync(cancellationToken);
        return (CalendarResponseMapper.MapRule(rule), calendarEvent.Version);
    }
}
