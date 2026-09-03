using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Calendar.Application.UseCases.Events;

public interface ICreateEventUseCase
{
    Task<CalendarEventDetailResponse> CreateEventAsync(CreateEventRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed class CreateEventUseCase(
    IEventRepository eventRepository,
    IRecurrenceEngine recurrenceEngine,
    IReminderSchedulePlanner reminderSchedulePlanner,
    IPrincipalAvailabilityQuery principalAvailabilityQuery,
    TimeProvider timeProvider) : ICreateEventUseCase
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
            var valid = await principalAvailabilityQuery.ArePrincipalsAvailableAsync(request.AudiencePrincipalIds, cancellationToken);
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
            ReminderRuleValidator.Validate(
                ruleReq.RemindBeforeMinutes,
                ruleReq.RepeatEveryMinutes,
                ruleReq.RepeatCount,
                ruleReq.MaxLatenessMinutes);

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

        await eventRepository.AddEventAsync(calendarEvent, cancellationToken);
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
}
