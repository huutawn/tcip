namespace TCIP.Business.Modules.Calendar.Application.Contracts;

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
