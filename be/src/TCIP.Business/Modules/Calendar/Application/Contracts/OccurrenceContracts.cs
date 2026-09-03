using System.ComponentModel.DataAnnotations;
using TCIP.Business.Modules.Calendar.Domain.Enums;

namespace TCIP.Business.Modules.Calendar.Application.Contracts;

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
