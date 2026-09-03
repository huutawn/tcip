using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Events;

public interface ICancelEventUseCase
{
    Task<long> CancelEventAsync(Guid eventId, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed class CancelEventUseCase(
    IEventRepository eventRepository,
    TimeProvider timeProvider) : ICancelEventUseCase
{
    public async Task<long> CancelEventAsync(
        Guid eventId,
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
            "Only the event creator can cancel this event.");

        if (calendarEvent.Status == EventStatus.Cancelled)
        {
            return calendarEvent.Version;
        }

        var now = timeProvider.GetUtcNow();
        calendarEvent.Status = EventStatus.Cancelled;
        calendarEvent.Version++;
        calendarEvent.UpdatedAtUtc = now;

        foreach (var rule in calendarEvent.ReminderRules)
        {
            rule.Status = ReminderRuleStatus.Cancelled;
            rule.UpdatedAtUtc = now;
            if (rule.Schedule is not null)
            {
                rule.Schedule.Status = ReminderScheduleStatus.Cancelled;
                rule.Schedule.EventVersion = calendarEvent.Version;
                rule.Schedule.UpdatedAtUtc = now;
            }
        }

        await eventRepository.SaveChangesAsync(cancellationToken);
        return calendarEvent.Version;
    }
}
