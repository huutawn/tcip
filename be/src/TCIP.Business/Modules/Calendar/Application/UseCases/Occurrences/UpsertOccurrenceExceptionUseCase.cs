using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Occurrences;

public interface IUpsertOccurrenceExceptionUseCase
{
    Task<(OccurrenceExceptionResponse Response, long NewVersion)> UpsertOccurrenceExceptionAsync(
        Guid eventId,
        UpsertOccurrenceExceptionRequest request,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class UpsertOccurrenceExceptionUseCase(
    IEventRepository eventRepository,
    IRecurrenceEngine recurrenceEngine,
    IReminderSchedulePlanner reminderSchedulePlanner,
    TimeProvider timeProvider) : IUpsertOccurrenceExceptionUseCase
{
    public async Task<(OccurrenceExceptionResponse Response, long NewVersion)> UpsertOccurrenceExceptionAsync(
        Guid eventId,
        UpsertOccurrenceExceptionRequest request,
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

        var resolvedOriginal = recurrenceEngine.ResolveOriginalOccurrence(calendarEvent, request.OriginalStartAtUtc);
        if (resolvedOriginal is null && !calendarEvent.OccurrenceExceptions.Any(x => x.OriginalStartAtUtc == request.OriginalStartAtUtc))
        {
            throw new BadRequestException($"OriginalStartAtUtc '{request.OriginalStartAtUtc}' does not belong to the event series.");
        }

        if (request.IsCancelled)
        {
            if (request.StartAtUtc.HasValue || request.EndAtUtc.HasValue)
            {
                throw new BadRequestException("Cancelled occurrence exceptions cannot have override start or end times.");
            }
        }
        else
        {
            if (!request.StartAtUtc.HasValue && !request.EndAtUtc.HasValue)
            {
                throw new BadRequestException("Occurrence exception must specify cancellation or override start/end times.");
            }

            var effStart = request.StartAtUtc ?? request.OriginalStartAtUtc;
            if (request.EndAtUtc.HasValue && request.EndAtUtc.Value <= effStart)
            {
                throw new BadRequestException("Explicit override EndAt must be strictly after effective StartAt.");
            }
        }

        var now = timeProvider.GetUtcNow();
        var ex = calendarEvent.OccurrenceExceptions.FirstOrDefault(x => x.OriginalStartAtUtc == request.OriginalStartAtUtc);

        if (ex is not null &&
            ex.IsCancelled == request.IsCancelled &&
            ex.StartAtUtc == request.StartAtUtc &&
            ex.EndAtUtc == request.EndAtUtc)
        {
            return (CalendarResponseMapper.MapException(ex), calendarEvent.Version);
        }

        if (ex is null)
        {
            ex = new EventOccurrenceException
            {
                EventId = eventId,
                OriginalStartAtUtc = request.OriginalStartAtUtc,
                IsCancelled = request.IsCancelled,
                StartAtUtc = request.StartAtUtc,
                EndAtUtc = request.EndAtUtc,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            calendarEvent.OccurrenceExceptions.Add(ex);
        }
        else
        {
            ex.IsCancelled = request.IsCancelled;
            ex.StartAtUtc = request.StartAtUtc;
            ex.EndAtUtc = request.EndAtUtc;
            ex.UpdatedAtUtc = now;
        }

        calendarEvent.Version++;
        calendarEvent.UpdatedAtUtc = now;

        foreach (var rule in calendarEvent.ReminderRules.Where(r => r.Status == ReminderRuleStatus.Active))
        {
            reminderSchedulePlanner.ReprojectSchedule(rule, calendarEvent, now);
        }

        await eventRepository.SaveChangesAsync(cancellationToken);
        return (CalendarResponseMapper.MapException(ex), calendarEvent.Version);
    }
}
