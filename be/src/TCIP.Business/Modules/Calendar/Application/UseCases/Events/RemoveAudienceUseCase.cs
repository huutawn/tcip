using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Events;

public interface IRemoveAudienceUseCase
{
    Task<long> RemoveAudienceAsync(Guid eventId, Guid principalId, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed class RemoveAudienceUseCase(
    IEventRepository eventRepository,
    IReminderSchedulePlanner reminderSchedulePlanner,
    TimeProvider timeProvider) : IRemoveAudienceUseCase
{
    public async Task<long> RemoveAudienceAsync(
        Guid eventId,
        Guid principalId,
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
            "Only the event creator can modify event audiences.");

        var existingAudience = calendarEvent.Audiences.FirstOrDefault(a => a.PrincipalId == principalId);
        if (existingAudience is null || existingAudience.Status == EventAudienceStatus.Removed)
        {
            return calendarEvent.Version;
        }

        var now = timeProvider.GetUtcNow();
        existingAudience.Status = EventAudienceStatus.Removed;
        calendarEvent.Version++;
        calendarEvent.UpdatedAtUtc = now;

        foreach (var rule in calendarEvent.ReminderRules)
        {
            if (rule.Schedule is not null)
            {
                reminderSchedulePlanner.UpdateScheduleVersion(rule.Schedule, calendarEvent.Version, now);
            }
        }

        await eventRepository.SaveChangesAsync(cancellationToken);
        return calendarEvent.Version;
    }
}
