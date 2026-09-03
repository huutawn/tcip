using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Events;

public interface IUpdateEventUseCase
{
    Task<CalendarEventDetailResponse> UpdateEventAsync(Guid eventId, UpdateEventRequest request, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed class UpdateEventUseCase(
    IEventRepository eventRepository,
    IRecurrenceEngine recurrenceEngine,
    IReminderSchedulePlanner reminderSchedulePlanner,
    TimeProvider timeProvider) : IUpdateEventUseCase
{
    public async Task<CalendarEventDetailResponse> UpdateEventAsync(
        Guid eventId,
        UpdateEventRequest request,
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
            "Only the event creator can modify this event.");

        if (request.EndAt.HasValue && request.EndAt.Value <= request.StartAt)
        {
            throw new BadRequestException("EndAt must be strictly after StartAt.");
        }

        var canonicalRule = recurrenceEngine.NormalizeAndValidateRule(
            request.RecurrenceRule,
            request.StartAt,
            request.TimeZoneId);

        ValidateTranslations(request.Translations);

        var now = timeProvider.GetUtcNow();
        var schedulingChanged = calendarEvent.StartAtUtc != request.StartAt ||
                                calendarEvent.EndAtUtc != request.EndAt ||
                                !string.Equals(calendarEvent.TimeZoneId, request.TimeZoneId.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                !string.Equals(calendarEvent.RecurrenceRule, canonicalRule, StringComparison.Ordinal);

        var translationsChanged = HaveTranslationsChanged(calendarEvent.Translations, request.Translations);

        if (!schedulingChanged && !translationsChanged)
        {
            return CalendarResponseMapper.MapDetail(calendarEvent);
        }

        calendarEvent.StartAtUtc = request.StartAt;
        calendarEvent.EndAtUtc = request.EndAt;
        calendarEvent.TimeZoneId = request.TimeZoneId.Trim();
        calendarEvent.RecurrenceRule = canonicalRule;
        calendarEvent.Version++;
        calendarEvent.UpdatedAtUtc = now;

        calendarEvent.Translations.Clear();
        foreach (var translation in request.Translations)
        {
            calendarEvent.Translations.Add(new EventTranslation
            {
                EventId = calendarEvent.Id,
                Language = translation.Language.Trim().ToLowerInvariant(),
                Title = translation.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(translation.Description) ? null : translation.Description.Trim()
            });
        }

        if (schedulingChanged)
        {
            var orphanExceptions = calendarEvent.OccurrenceExceptions
                .Where(ex => recurrenceEngine.ResolveOriginalOccurrence(calendarEvent, ex.OriginalStartAtUtc) is null)
                .ToList();

            foreach (var orphan in orphanExceptions)
            {
                calendarEvent.OccurrenceExceptions.Remove(orphan);
            }

            foreach (var rule in calendarEvent.ReminderRules.Where(r => r.Status == ReminderRuleStatus.Active))
            {
                reminderSchedulePlanner.InitializeOrRebuildSchedule(rule, calendarEvent, now, isRebuild: true);
            }
        }
        else
        {
            foreach (var rule in calendarEvent.ReminderRules)
            {
                if (rule.Schedule is not null)
                {
                    reminderSchedulePlanner.UpdateScheduleVersion(rule.Schedule, calendarEvent.Version, now);
                }
            }
        }

        await eventRepository.SaveChangesAsync(cancellationToken);
        return CalendarResponseMapper.MapDetail(calendarEvent);
    }

    private static void ValidateTranslations(IReadOnlyList<EventTranslationRequest> translations)
    {
        if (translations.Count == 0)
            throw new BadRequestException("At least one translation is required.");

        var languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in translations)
        {
            if (string.IsNullOrWhiteSpace(t.Language))
                throw new BadRequestException("Translation language is required.");
            if (string.IsNullOrWhiteSpace(t.Title))
                throw new BadRequestException("Translation title is required.");

            if (!languages.Add(t.Language.Trim()))
            {
                throw new BadRequestException($"Duplicate translation language: '{t.Language}'.");
            }
        }
    }

    private static bool HaveTranslationsChanged(
        ICollection<EventTranslation> current,
        IReadOnlyList<EventTranslationRequest> requested)
    {
        if (current.Count != requested.Count)
            return true;

        var map = current.ToDictionary(x => x.Language, StringComparer.OrdinalIgnoreCase);
        foreach (var req in requested)
        {
            if (!map.TryGetValue(req.Language.Trim(), out var existing))
                return true;

            if (!string.Equals(existing.Title, req.Title.Trim(), StringComparison.Ordinal) ||
                !string.Equals(existing.Description, req.Description?.Trim(), StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
