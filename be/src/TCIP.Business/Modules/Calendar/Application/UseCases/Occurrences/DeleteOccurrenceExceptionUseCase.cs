using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Occurrences;

public interface IDeleteOccurrenceExceptionUseCase
{
    Task<long> DeleteOccurrenceExceptionAsync(
        Guid eventId,
        DateTimeOffset originalStartAtUtc,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class DeleteOccurrenceExceptionUseCase(
    IEventRepository eventRepository,
    IReminderSchedulePlanner reminderSchedulePlanner,
    TimeProvider timeProvider) : IDeleteOccurrenceExceptionUseCase
{
    public async Task<long> DeleteOccurrenceExceptionAsync(
        Guid eventId,
        DateTimeOffset originalStartAtUtc,
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
            "Only the event creator can manage occurrence exceptions.");

        var ex = calendarEvent.OccurrenceExceptions.FirstOrDefault(x => x.OriginalStartAtUtc == originalStartAtUtc);
        if (ex is null)
        {
            return calendarEvent.Version;
        }

        var now = timeProvider.GetUtcNow();
        calendarEvent.OccurrenceExceptions.Remove(ex);

        calendarEvent.Version++;
        calendarEvent.UpdatedAtUtc = now;

        foreach (var rule in calendarEvent.ReminderRules.Where(r => r.Status == ReminderRuleStatus.Active))
        {
            reminderSchedulePlanner.ReprojectSchedule(rule, calendarEvent, now);
        }

        await eventRepository.SaveChangesAsync(cancellationToken);
        return calendarEvent.Version;
    }
}
