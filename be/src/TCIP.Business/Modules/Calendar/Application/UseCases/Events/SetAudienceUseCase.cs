using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Events;

public interface ISetAudienceUseCase
{
    Task<long> SetAudienceAsync(Guid eventId, Guid principalId, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed class SetAudienceUseCase(
    IEventRepository eventRepository,
    IReminderSchedulePlanner reminderSchedulePlanner,
    IPrincipalAvailabilityQuery principalAvailabilityQuery,
    TimeProvider timeProvider) : ISetAudienceUseCase
{
    public async Task<long> SetAudienceAsync(
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

        var valid = await principalAvailabilityQuery.ArePrincipalsAvailableAsync([principalId], cancellationToken);
        if (!valid)
        {
            throw new BadRequestException($"Principal with ID '{principalId}' does not exist or is unavailable.");
        }

        var existingAudience = calendarEvent.Audiences.FirstOrDefault(a => a.PrincipalId == principalId);
        if (existingAudience is not null && existingAudience.Status == EventAudienceStatus.Active)
        {
            return calendarEvent.Version;
        }

        var now = timeProvider.GetUtcNow();
        if (existingAudience is null)
        {
            calendarEvent.Audiences.Add(new EventAudience
            {
                EventId = eventId,
                PrincipalId = principalId,
                Status = EventAudienceStatus.Active
            });
        }
        else
        {
            existingAudience.Status = EventAudienceStatus.Active;
        }

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
