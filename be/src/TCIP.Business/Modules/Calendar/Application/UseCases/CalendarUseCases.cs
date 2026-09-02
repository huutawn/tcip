using System.Text;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Calendar.Application.UseCases;

public interface IEventCommandUseCase
{
    Task<CalendarEventDetailResponse> CreateEventAsync(CreateEventRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<CalendarEventDetailResponse> GetEventDetailAsync(Guid eventId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<CalendarEventDetailResponse> UpdateEventAsync(Guid eventId, UpdateEventRequest request, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<long> CancelEventAsync(Guid eventId, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<long> SetAudienceAsync(Guid eventId, Guid principalId, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<long> RemoveAudienceAsync(Guid eventId, Guid principalId, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed class EventCommandUseCase(
    ICalendarRepository calendarRepository,
    IRecurrenceEngine recurrenceEngine,
    IReminderSchedulePlanner reminderSchedulePlanner,
    TimeProvider timeProvider) : IEventCommandUseCase
{
    public async Task<CalendarEventDetailResponse> CreateEventAsync(
        CreateEventRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        if (request.EndAt.HasValue && request.EndAt.Value <= request.StartAt)
        {
            throw new BadRequestException("EndAt must be strictly after StartAt.");
        }

        var canonicalRule = recurrenceEngine.NormalizeAndValidateRule(
            request.RecurrenceRule,
            request.StartAt,
            request.TimeZoneId);

        ValidateTranslations(request.Translations);

        if (request.AudiencePrincipalIds.Count > 0)
        {
            var valid = await calendarRepository.PrincipalsExistAndAvailableAsync(request.AudiencePrincipalIds, cancellationToken);
            if (!valid)
            {
                throw new BadRequestException("One or more audience principals do not exist or are unavailable.");
            }
        }

        var calendarEvent = new Event
        {
            Id = Guid.NewGuid(),
            CreatedById = actorUserId,
            StartAtUtc = request.StartAt,
            EndAtUtc = request.EndAt,
            TimeZoneId = request.TimeZoneId.Trim(),
            RecurrenceRule = canonicalRule,
            Status = EventStatus.Active,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

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

        foreach (var principalId in request.AudiencePrincipalIds.Distinct())
        {
            calendarEvent.Audiences.Add(new EventAudience
            {
                EventId = calendarEvent.Id,
                PrincipalId = principalId,
                Status = EventAudienceStatus.Active
            });
        }

        foreach (var ruleReq in request.ReminderRules)
        {
            ValidateReminderRule(ruleReq);

            var ruleId = Guid.NewGuid();
            var rule = new ReminderRule
            {
                Id = ruleId,
                EventId = calendarEvent.Id,
                RemindBeforeMinutes = ruleReq.RemindBeforeMinutes,
                RepeatEveryMinutes = ruleReq.RepeatEveryMinutes,
                RepeatCount = ruleReq.RepeatCount,
                MisfirePolicy = ruleReq.MisfirePolicy,
                MaxLatenessMinutes = ruleReq.MaxLatenessMinutes,
                Status = ReminderRuleStatus.Active,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            reminderSchedulePlanner.InitializeOrRebuildSchedule(rule, calendarEvent, now, isRebuild: false);
            calendarEvent.ReminderRules.Add(rule);
        }

        await calendarRepository.AddEventAsync(calendarEvent, cancellationToken);
        return MapDetail(calendarEvent);
    }

    public async Task<CalendarEventDetailResponse> GetEventDetailAsync(
        Guid eventId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await calendarRepository.GetEventByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{eventId}' not found.");

        var user = await calendarRepository.GetUserAsync(actorUserId, cancellationToken);
        var isCreator = calendarEvent.CreatedById == actorUserId;
        var isAudience = user != null && calendarEvent.Audiences.Any(a =>
            a.Status == EventAudienceStatus.Active &&
            (a.PrincipalId == user.PrincipalId ||
             (a.Principal != null && a.Principal.Type != PrincipalType.User)));

        if (!isCreator && !isAudience)
        {
            throw new NotFoundException($"Event with ID '{eventId}' not found.");
        }

        return MapDetail(calendarEvent);
    }

    public async Task<CalendarEventDetailResponse> UpdateEventAsync(
        Guid eventId,
        UpdateEventRequest request,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await calendarRepository.GetEventForUpdateAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{eventId}' not found.");

        if (calendarEvent.CreatedById != actorUserId)
        {
            throw new ForbiddenException("Only the event creator can modify this event.");
        }

        if (calendarEvent.Version != expectedVersion)
        {
            throw new PreconditionFailedException($"Precondition Failed: Resource version '{calendarEvent.Version}' does not match expected version '{expectedVersion}'.");
        }

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
            return MapDetail(calendarEvent);
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

        await calendarRepository.SaveChangesAsync(cancellationToken);
        return MapDetail(calendarEvent);
    }

    public async Task<long> CancelEventAsync(
        Guid eventId,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await calendarRepository.GetEventForUpdateAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{eventId}' not found.");

        if (calendarEvent.CreatedById != actorUserId)
        {
            throw new ForbiddenException("Only the event creator can cancel this event.");
        }

        if (calendarEvent.Version != expectedVersion)
        {
            throw new PreconditionFailedException($"Precondition Failed: Resource version '{calendarEvent.Version}' does not match expected version '{expectedVersion}'.");
        }

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

        await calendarRepository.SaveChangesAsync(cancellationToken);
        return calendarEvent.Version;
    }

    public async Task<long> SetAudienceAsync(
        Guid eventId,
        Guid principalId,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await calendarRepository.GetEventForUpdateAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{eventId}' not found.");

        if (calendarEvent.CreatedById != actorUserId)
        {
            throw new ForbiddenException("Only the event creator can modify event audiences.");
        }

        if (calendarEvent.Version != expectedVersion)
        {
            throw new PreconditionFailedException($"Precondition Failed: Resource version '{calendarEvent.Version}' does not match expected version '{expectedVersion}'.");
        }

        var valid = await calendarRepository.PrincipalsExistAndAvailableAsync([principalId], cancellationToken);
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

        await calendarRepository.SaveChangesAsync(cancellationToken);
        return calendarEvent.Version;
    }

    public async Task<long> RemoveAudienceAsync(
        Guid eventId,
        Guid principalId,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await calendarRepository.GetEventForUpdateAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{eventId}' not found.");

        if (calendarEvent.CreatedById != actorUserId)
        {
            throw new ForbiddenException("Only the event creator can modify event audiences.");
        }

        if (calendarEvent.Version != expectedVersion)
        {
            throw new PreconditionFailedException($"Precondition Failed: Resource version '{calendarEvent.Version}' does not match expected version '{expectedVersion}'.");
        }

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

        await calendarRepository.SaveChangesAsync(cancellationToken);
        return calendarEvent.Version;
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

    private static void ValidateReminderRule(CreateReminderRuleRequest rule)
    {
        if (rule.RemindBeforeMinutes < 0)
            throw new BadRequestException("RemindBeforeMinutes cannot be negative.");
        if (rule.RepeatCount < 0)
            throw new BadRequestException("RepeatCount cannot be negative.");
        if (rule.MaxLatenessMinutes < 0)
            throw new BadRequestException("MaxLatenessMinutes cannot be negative.");

        if (rule.RepeatCount > 0)
        {
            if (!rule.RepeatEveryMinutes.HasValue || rule.RepeatEveryMinutes.Value <= 0)
                throw new BadRequestException("RepeatEveryMinutes is required and must be positive when RepeatCount > 0.");

            var totalRepeatMinutes = (long)rule.RepeatCount * rule.RepeatEveryMinutes.Value;
            if (totalRepeatMinutes >= rule.RemindBeforeMinutes)
                throw new BadRequestException("The last reminder repeat must fire strictly before the event occurrence start.");
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

    private static CalendarEventDetailResponse MapDetail(Event ev) => new(
        ev.Id,
        ev.CreatedById,
        ev.StartAtUtc,
        ev.EndAtUtc,
        ev.TimeZoneId,
        ev.RecurrenceRule,
        ev.Status,
        ev.Version,
        ev.Translations.Select(t => new EventTranslationResponse(t.Language, t.Title, t.Description)).ToList(),
        ev.Audiences.Where(a => a.Status == EventAudienceStatus.Active).Select(a => new EventAudienceResponse(
            a.PrincipalId,
            a.Principal?.Type.ToString(),
            a.Principal?.User?.DisplayName ?? a.Principal?.Group?.Name ?? a.Principal?.Team?.Name ?? a.Principal?.Project?.Name ?? a.Principal?.Department?.Name,
            a.Status)).ToList(),
        ev.ReminderRules.Where(r => r.Status == ReminderRuleStatus.Active).Select(r => new ReminderRuleResponse(
            r.Id,
            r.EventId,
            r.RemindBeforeMinutes,
            r.RepeatEveryMinutes,
            r.RepeatCount,
            r.MisfirePolicy,
            r.MaxLatenessMinutes,
            r.Status,
            r.CreatedAtUtc,
            r.UpdatedAtUtc)).ToList(),
        ev.OccurrenceExceptions.Select(ex => new OccurrenceExceptionResponse(
            ex.OriginalStartAtUtc,
            ex.IsCancelled,
            ex.StartAtUtc,
            ex.EndAtUtc,
            ex.UpdatedAtUtc)).ToList(),
        ev.CreatedAtUtc,
        ev.UpdatedAtUtc);
}

public interface IReminderRuleUseCase
{
    Task<(ReminderRuleResponse Response, long NewVersion)> AddReminderRuleAsync(Guid eventId, CreateReminderRuleRequest request, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<(ReminderRuleResponse Response, long NewVersion)> UpdateReminderRuleAsync(Guid eventId, Guid ruleId, UpdateReminderRuleRequest request, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<long> DeleteReminderRuleAsync(Guid eventId, Guid ruleId, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed class ReminderRuleUseCase(
    ICalendarRepository calendarRepository,
    IReminderSchedulePlanner reminderSchedulePlanner,
    TimeProvider timeProvider) : IReminderRuleUseCase
{
    public async Task<(ReminderRuleResponse Response, long NewVersion)> AddReminderRuleAsync(
        Guid eventId,
        CreateReminderRuleRequest request,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await calendarRepository.GetEventForUpdateAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{eventId}' not found.");

        if (calendarEvent.CreatedById != actorUserId)
        {
            throw new ForbiddenException("Only the event creator can manage reminder rules.");
        }

        if (calendarEvent.Version != expectedVersion)
        {
            throw new PreconditionFailedException($"Precondition Failed: Resource version '{calendarEvent.Version}' does not match expected version '{expectedVersion}'.");
        }

        ValidateReminderRule(request);

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
            RepeatCount = request.RepeatCount,
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

        await calendarRepository.SaveChangesAsync(cancellationToken);
        return (MapRule(rule), calendarEvent.Version);
    }

    public async Task<(ReminderRuleResponse Response, long NewVersion)> UpdateReminderRuleAsync(
        Guid eventId,
        Guid ruleId,
        UpdateReminderRuleRequest request,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await calendarRepository.GetEventForUpdateAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{eventId}' not found.");

        if (calendarEvent.CreatedById != actorUserId)
        {
            throw new ForbiddenException("Only the event creator can manage reminder rules.");
        }

        if (calendarEvent.Version != expectedVersion)
        {
            throw new PreconditionFailedException($"Precondition Failed: Resource version '{calendarEvent.Version}' does not match expected version '{expectedVersion}'.");
        }

        var rule = calendarEvent.ReminderRules.FirstOrDefault(r => r.Id == ruleId)
            ?? throw new NotFoundException($"Reminder rule with ID '{ruleId}' not found on event '{eventId}'.");

        ValidateReminderRule(new CreateReminderRuleRequest(
            request.RemindBeforeMinutes,
            request.RepeatEveryMinutes,
            request.RepeatCount,
            request.MisfirePolicy,
            request.MaxLatenessMinutes));

        var statusUnchanged = !request.Status.HasValue || request.Status.Value == rule.Status;
        var ruleUnchanged = rule.RemindBeforeMinutes == request.RemindBeforeMinutes &&
                            rule.RepeatEveryMinutes == request.RepeatEveryMinutes &&
                            rule.RepeatCount == request.RepeatCount &&
                            rule.MisfirePolicy == request.MisfirePolicy &&
                            rule.MaxLatenessMinutes == request.MaxLatenessMinutes &&
                            statusUnchanged;

        if (ruleUnchanged)
        {
            return (MapRule(rule), calendarEvent.Version);
        }

        var now = timeProvider.GetUtcNow();
        rule.RemindBeforeMinutes = request.RemindBeforeMinutes;
        rule.RepeatEveryMinutes = request.RepeatEveryMinutes;
        rule.RepeatCount = request.RepeatCount;
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

        await calendarRepository.SaveChangesAsync(cancellationToken);
        return (MapRule(rule), calendarEvent.Version);
    }

    public async Task<long> DeleteReminderRuleAsync(
        Guid eventId,
        Guid ruleId,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await calendarRepository.GetEventForUpdateAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{eventId}' not found.");

        if (calendarEvent.CreatedById != actorUserId)
        {
            throw new ForbiddenException("Only the event creator can manage reminder rules.");
        }

        if (calendarEvent.Version != expectedVersion)
        {
            throw new PreconditionFailedException($"Precondition Failed: Resource version '{calendarEvent.Version}' does not match expected version '{expectedVersion}'.");
        }

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

        await calendarRepository.SaveChangesAsync(cancellationToken);
        return calendarEvent.Version;
    }

    private static void ValidateReminderRule(CreateReminderRuleRequest rule)
    {
        if (rule.RemindBeforeMinutes < 0)
            throw new BadRequestException("RemindBeforeMinutes cannot be negative.");
        if (rule.RepeatCount < 0)
            throw new BadRequestException("RepeatCount cannot be negative.");
        if (rule.MaxLatenessMinutes < 0)
            throw new BadRequestException("MaxLatenessMinutes cannot be negative.");

        if (rule.RepeatCount > 0)
        {
            if (!rule.RepeatEveryMinutes.HasValue || rule.RepeatEveryMinutes.Value <= 0)
                throw new BadRequestException("RepeatEveryMinutes is required and must be positive when RepeatCount > 0.");

            var totalRepeatMinutes = (long)rule.RepeatCount * rule.RepeatEveryMinutes.Value;
            if (totalRepeatMinutes >= rule.RemindBeforeMinutes)
                throw new BadRequestException("The last reminder repeat must fire strictly before the event occurrence start.");
        }
    }

    private static ReminderRuleResponse MapRule(ReminderRule r) => new(
        r.Id,
        r.EventId,
        r.RemindBeforeMinutes,
        r.RepeatEveryMinutes,
        r.RepeatCount,
        r.MisfirePolicy,
        r.MaxLatenessMinutes,
        r.Status,
        r.CreatedAtUtc,
        r.UpdatedAtUtc);
}

public interface IEventOccurrenceUseCase
{
    Task<(OccurrenceExceptionResponse Response, long NewVersion)> UpsertOccurrenceExceptionAsync(Guid eventId, UpsertOccurrenceExceptionRequest request, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<long> DeleteOccurrenceExceptionAsync(Guid eventId, DateTimeOffset originalStartAtUtc, long expectedVersion, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<CalendarEventsByDayResponse> GetEventsByDayAsync(Guid userId, DateTimeOffset? day, string? cursor, int limit = 200, CancellationToken cancellationToken = default);
}

public sealed class EventOccurrenceUseCase(
    ICalendarRepository calendarRepository,
    IRecurrenceEngine recurrenceEngine,
    IReminderSchedulePlanner reminderSchedulePlanner,
    TimeProvider timeProvider) : IEventOccurrenceUseCase
{
    public async Task<(OccurrenceExceptionResponse Response, long NewVersion)> UpsertOccurrenceExceptionAsync(
        Guid eventId,
        UpsertOccurrenceExceptionRequest request,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await calendarRepository.GetEventForUpdateAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{eventId}' not found.");

        if (calendarEvent.CreatedById != actorUserId)
        {
            throw new ForbiddenException("Only the event creator can manage occurrence exceptions.");
        }

        if (calendarEvent.Version != expectedVersion)
        {
            throw new PreconditionFailedException($"Precondition Failed: Resource version '{calendarEvent.Version}' does not match expected version '{expectedVersion}'.");
        }

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
            return (MapException(ex), calendarEvent.Version);
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

        await calendarRepository.SaveChangesAsync(cancellationToken);
        return (MapException(ex), calendarEvent.Version);
    }

    public async Task<long> DeleteOccurrenceExceptionAsync(
        Guid eventId,
        DateTimeOffset originalStartAtUtc,
        long expectedVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await calendarRepository.GetEventForUpdateAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{eventId}' not found.");

        if (calendarEvent.CreatedById != actorUserId)
        {
            throw new ForbiddenException("Only the event creator can manage occurrence exceptions.");
        }

        if (calendarEvent.Version != expectedVersion)
        {
            throw new PreconditionFailedException($"Precondition Failed: Resource version '{calendarEvent.Version}' does not match expected version '{expectedVersion}'.");
        }

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

        await calendarRepository.SaveChangesAsync(cancellationToken);
        return calendarEvent.Version;
    }

    public async Task<CalendarEventsByDayResponse> GetEventsByDayAsync(
        Guid userId,
        DateTimeOffset? day,
        string? cursor,
        int limit = 200,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000)
        {
            limit = Math.Clamp(limit, 1, 1000);
        }

        var user = await calendarRepository.GetUserAsync(userId, cancellationToken);
        var userTz = recurrenceEngine.GetTimeZone(user?.TimeZoneId ?? "UTC");
        var userLanguage = user?.Language ?? "en";

        var refTime = day ?? timeProvider.GetUtcNow();
        var userLocalRef = TimeZoneInfo.ConvertTime(refTime, userTz).DateTime;

        var localMidnightStart = new DateTime(userLocalRef.Year, userLocalRef.Month, userLocalRef.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var localMidnightEnd = localMidnightStart.AddDays(1);

        var dayStartUtc = ToUtc(localMidnightStart, userTz);
        var dayEndUtc = ToUtc(localMidnightEnd, userTz);

        var events = await calendarRepository.GetEventsForDayWindowAsync(userId, dayStartUtc, dayEndUtc, cancellationToken);
        var allOccurrences = new List<EventOccurrenceResponse>();

        foreach (var ev in events)
        {
            var occurrences = recurrenceEngine.ExpandWindow(
                ev,
                dayStartUtc,
                dayEndUtc,
                ev.OccurrenceExceptions.ToList());

            var (title, description) = SelectTranslation(ev.Translations, userLanguage);

            foreach (var occ in occurrences)
            {
                allOccurrences.Add(new EventOccurrenceResponse(
                    ev.Id,
                    occ.OriginalStartAtUtc,
                    occ.StartAtUtc,
                    occ.EndAtUtc,
                    title,
                    description,
                    ev.TimeZoneId,
                    ev.Status,
                    ev.Version));
            }
        }

        var sorted = allOccurrences
            .OrderBy(x => x.StartAt)
            .ThenBy(x => x.EventId)
            .ThenBy(x => x.OriginalStartAt)
            .ToList();

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            var parsed = DecodeCursor(cursor);
            sorted = sorted
                .Where(x => x.StartAt > parsed.StartAt ||
                            (x.StartAt == parsed.StartAt && x.EventId.CompareTo(parsed.EventId) > 0) ||
                            (x.StartAt == parsed.StartAt && x.EventId == parsed.EventId && x.OriginalStartAt > parsed.OriginalStartAt))
                .ToList();
        }

        var hasNextPage = sorted.Count > limit;
        var pagedItems = sorted.Take(limit).ToList();
        var nextCursor = hasNextPage && pagedItems.Count > 0
            ? EncodeCursor(pagedItems[^1])
            : null;

        return new CalendarEventsByDayResponse(pagedItems, nextCursor);
    }

    private static (string Title, string? Description) SelectTranslation(
        ICollection<EventTranslation> translations,
        string userLanguage)
    {
        if (translations.Count == 0)
            return (string.Empty, null);

        var exact = translations.FirstOrDefault(t => string.Equals(t.Language, userLanguage, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return (exact.Title, exact.Description);

        var baseUserLang = userLanguage.Split('-')[0];
        var baseMatch = translations.FirstOrDefault(t => string.Equals(t.Language.Split('-')[0], baseUserLang, StringComparison.OrdinalIgnoreCase));
        if (baseMatch is not null)
            return (baseMatch.Title, baseMatch.Description);

        var english = translations.FirstOrDefault(t => string.Equals(t.Language, "en", StringComparison.OrdinalIgnoreCase));
        if (english is not null)
            return (english.Title, english.Description);

        var first = translations.First();
        return (first.Title, first.Description);
    }

    private static DateTimeOffset ToUtc(DateTime localDateTime, TimeZoneInfo timeZone)
    {
        if (timeZone.IsInvalidTime(localDateTime))
        {
            var adjusted = localDateTime;
            for (var i = 1; i <= 8; i++)
            {
                adjusted = localDateTime.AddMinutes(15 * i);
                if (!timeZone.IsInvalidTime(adjusted))
                {
                    localDateTime = adjusted;
                    break;
                }
            }
        }

        if (timeZone.IsAmbiguousTime(localDateTime))
        {
            var offsets = timeZone.GetAmbiguousTimeOffsets(localDateTime);
            var standardOffset = offsets.Min();
            return new DateTimeOffset(localDateTime, standardOffset).ToUniversalTime();
        }

        var offset = timeZone.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset).ToUniversalTime();
    }

    private static string EncodeCursor(EventOccurrenceResponse occ)
    {
        var raw = $"v1|{occ.StartAt.ToUnixTimeMilliseconds()}|{occ.EventId:N}|{occ.OriginalStartAt.ToUnixTimeMilliseconds()}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static (DateTimeOffset StartAt, Guid EventId, DateTimeOffset OriginalStartAt) DecodeCursor(string cursor)
    {
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('|');
            if (parts.Length != 4 || parts[0] != "v1")
            {
                throw new BadRequestException("Invalid cursor format.");
            }

            var startAtMs = long.Parse(parts[1]);
            var eventId = Guid.Parse(parts[2]);
            var origStartAtMs = long.Parse(parts[3]);

            return (
                DateTimeOffset.FromUnixTimeMilliseconds(startAtMs),
                eventId,
                DateTimeOffset.FromUnixTimeMilliseconds(origStartAtMs));
        }
        catch (Exception ex) when (ex is not BadRequestException)
        {
            throw new BadRequestException("Invalid cursor.");
        }
    }

    private static OccurrenceExceptionResponse MapException(EventOccurrenceException ex) => new(
        ex.OriginalStartAtUtc,
        ex.IsCancelled,
        ex.StartAtUtc,
        ex.EndAtUtc,
        ex.UpdatedAtUtc);
}

public interface INotificationQueryUseCase
{
    Task<IReadOnlyList<NotificationResponse>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> MarkNotificationReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
}

public sealed class NotificationQueryUseCase(
    ICalendarRepository calendarRepository,
    TimeProvider timeProvider) : INotificationQueryUseCase
{
    public async Task<IReadOnlyList<NotificationResponse>> GetNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var list = await calendarRepository.GetNotificationsAsync(userId, cancellationToken);
        return list.Select(n => new NotificationResponse(
            n.Id,
            n.EventId,
            n.ReminderRuleId,
            n.OriginalStartAtUtc,
            n.EffectiveStartAtUtc,
            n.ScheduledFireAtUtc,
            n.RepeatIndex,
            n.Title,
            n.Description,
            n.SentAtUtc,
            n.ReadAtUtc)).ToList();
    }

    public async Task<bool> MarkNotificationReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await calendarRepository.GetNotificationForUpdateAsync(userId, notificationId, cancellationToken);
        if (notification is null)
        {
            return false;
        }

        if (!notification.ReadAtUtc.HasValue)
        {
            notification.ReadAtUtc = timeProvider.GetUtcNow();
            await calendarRepository.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
