using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Reminders;

public interface IAddReminderRuleUseCase
{
    Task<(ReminderRuleResponse Response, long NewVersion)> AddReminderRuleAsync(
        Guid eventId,
        CreateReminderRuleRequest request,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class AddReminderRuleUseCase(
    IEventRepository eventRepository,
    IReminderSchedulePlanner reminderSchedulePlanner,
    TimeProvider timeProvider) : IAddReminderRuleUseCase
{
    public async Task<(ReminderRuleResponse Response, long NewVersion)> AddReminderRuleAsync(
        Guid eventId,
        CreateReminderRuleRequest request,
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

        ReminderRuleValidator.Validate(
            request.RemindBeforeMinutes,
            request.RepeatEveryMinutes,
            request.MaxLatenessMinutes);

        var now = timeProvider.GetUtcNow();
        calendarEvent.Version++;
        calendarEvent.UpdatedAtUtc = now;

        var ruleId = Guid.NewGuid();
        var rule = new ReminderRule
        {
            Id = ruleId,
            EventId = eventId,
            RemindBeforeMinutes = request.RemindBeforeMinutes,
            RepeatEveryMinutes = request.RepeatEveryMinutes,
            MisfirePolicy = request.MisfirePolicy,
            MaxLatenessMinutes = request.MaxLatenessMinutes,
            Status = ReminderRuleStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        reminderSchedulePlanner.InitializeOrRebuildSchedule(rule, calendarEvent, now, isRebuild: false);
        calendarEvent.ReminderRules.Add(rule);

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
