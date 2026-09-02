using System.ComponentModel.DataAnnotations;
using TCIP.Business.Modules.Calendar.Domain.Enums;

namespace TCIP.Business.Modules.Calendar.Application.Contracts;

public sealed record CalendarEventDetailResponse(
    Guid Id,
    Guid CreatedById,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    string TimeZoneId,
    string? RecurrenceRule,
    EventStatus Status,
    long Version,
    IReadOnlyList<EventTranslationResponse> Translations,
    IReadOnlyList<EventAudienceResponse> Audiences,
    IReadOnlyList<ReminderRuleResponse> ReminderRules,
    IReadOnlyList<OccurrenceExceptionResponse> Exceptions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record EventTranslationRequest(
    [Required, MaxLength(16)] string Language,
    [Required, MaxLength(200)] string Title,
    [MaxLength(2000)] string? Description);

public sealed record EventTranslationResponse(
    string Language,
    string Title,
    string? Description);

public sealed record EventAudienceResponse(
    Guid PrincipalId,
    string? PrincipalType,
    string? PrincipalName,
    EventAudienceStatus Status);

public sealed record CreateEventRequest
{
    [Required]
    public DateTimeOffset StartAt { get; init; }

    public DateTimeOffset? EndAt { get; init; }

    [MaxLength(128)]
    public string TimeZoneId { get; init; } = "UTC";

    public string? RecurrenceRule { get; init; }

    [Required, MinLength(1)]
    public IReadOnlyList<EventTranslationRequest> Translations { get; init; } = [];

    public IReadOnlyList<Guid> AudiencePrincipalIds { get; init; } = [];

    public IReadOnlyList<CreateReminderRuleRequest> ReminderRules { get; init; } = [];
}

public sealed record UpdateEventRequest
{
    [Required]
    public DateTimeOffset StartAt { get; init; }

    public DateTimeOffset? EndAt { get; init; }

    [MaxLength(128)]
    public string TimeZoneId { get; init; } = "UTC";

    public string? RecurrenceRule { get; init; }

    [Required, MinLength(1)]
    public IReadOnlyList<EventTranslationRequest> Translations { get; init; } = [];
}

public sealed record CreateReminderRuleRequest(
    [Range(0, int.MaxValue)] int RemindBeforeMinutes,
    [Range(1, int.MaxValue)] int? RepeatEveryMinutes = null,
    [Range(0, int.MaxValue)] int RepeatCount = 0,
    MisfirePolicy MisfirePolicy = MisfirePolicy.FireOnceNow,
    [Range(0, int.MaxValue)] int MaxLatenessMinutes = 15);

public sealed record UpdateReminderRuleRequest(
    [Range(0, int.MaxValue)] int RemindBeforeMinutes,
    [Range(1, int.MaxValue)] int? RepeatEveryMinutes = null,
    [Range(0, int.MaxValue)] int RepeatCount = 0,
    MisfirePolicy MisfirePolicy = MisfirePolicy.FireOnceNow,
    [Range(0, int.MaxValue)] int MaxLatenessMinutes = 15,
    ReminderRuleStatus? Status = null);

public sealed record ReminderRuleResponse(
    Guid Id,
    Guid EventId,
    int RemindBeforeMinutes,
    int? RepeatEveryMinutes,
    int RepeatCount,
    MisfirePolicy MisfirePolicy,
    int MaxLatenessMinutes,
    ReminderRuleStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpsertOccurrenceExceptionRequest(
    [Required] DateTimeOffset OriginalStartAtUtc,
    bool IsCancelled,
    DateTimeOffset? StartAtUtc = null,
    DateTimeOffset? EndAtUtc = null);

public sealed record OccurrenceExceptionResponse(
    DateTimeOffset OriginalStartAtUtc,
    bool IsCancelled,
    DateTimeOffset? StartAtUtc,
    DateTimeOffset? EndAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CalendarEventsByDayResponse(
    IReadOnlyList<EventOccurrenceResponse> Items,
    string? NextCursor);

public sealed record EventOccurrenceResponse(
    Guid EventId,
    DateTimeOffset OriginalStartAt,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    string Title,
    string? Description,
    string TimeZoneId,
    EventStatus Status,
    long EventVersion);

public sealed record NotificationResponse(
    Guid Id,
    Guid EventId,
    Guid ReminderRuleId,
    DateTimeOffset OriginalStartAt,
    DateTimeOffset EffectiveStartAt,
    DateTimeOffset ScheduledFireAt,
    int RepeatIndex,
    string Title,
    string? Description,
    DateTimeOffset SentAt,
    DateTimeOffset? ReadAt);

public sealed record ReminderDueMessage(
    Guid MessageId,
    Guid ReminderRuleId,
    Guid EventId,
    long EventVersion,
    DateTimeOffset OriginalStartAtUtc,
    DateTimeOffset EffectiveStartAtUtc,
    DateTimeOffset ScheduledFireAtUtc,
    int RepeatIndex);

public sealed record NotificationBatchMessage(
    Guid BatchId,
    Guid ReminderRuleId,
    Guid EventId,
    long EventVersion,
    DateTimeOffset OriginalStartAtUtc,
    DateTimeOffset EffectiveStartAtUtc,
    DateTimeOffset ScheduledFireAtUtc,
    int RepeatIndex,
    IReadOnlyList<Guid> RecipientUserIds);
