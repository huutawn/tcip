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
