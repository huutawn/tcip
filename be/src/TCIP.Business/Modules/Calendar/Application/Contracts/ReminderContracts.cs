using System.ComponentModel.DataAnnotations;
using TCIP.Business.Modules.Calendar.Domain.Enums;

namespace TCIP.Business.Modules.Calendar.Application.Contracts;

public sealed record CreateReminderRuleRequest(
    [Range(0, int.MaxValue)] int RemindBeforeMinutes,
    [Range(1, int.MaxValue)] int? RepeatEveryMinutes = null,
    MisfirePolicy MisfirePolicy = MisfirePolicy.FireOnceNow,
    [Range(0, int.MaxValue)] int MaxLatenessMinutes = 15);

public sealed record UpdateReminderRuleRequest(
    [Range(0, int.MaxValue)] int RemindBeforeMinutes,
    [Range(1, int.MaxValue)] int? RepeatEveryMinutes = null,
    MisfirePolicy MisfirePolicy = MisfirePolicy.FireOnceNow,
    [Range(0, int.MaxValue)] int MaxLatenessMinutes = 15,
    ReminderRuleStatus? Status = null);

public sealed record ReminderRuleResponse(
    Guid Id,
    Guid EventId,
    int RemindBeforeMinutes,
    int? RepeatEveryMinutes,
    MisfirePolicy MisfirePolicy,
    int MaxLatenessMinutes,
    ReminderRuleStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ReminderDueMessage(
    Guid MessageId,
    Guid ReminderRuleId,
    Guid EventId,
    long EventVersion,
    DateTimeOffset OriginalStartAtUtc,
    DateTimeOffset EffectiveStartAtUtc,
    DateTimeOffset ScheduledFireAtUtc);

public sealed record NotificationBatchMessage(
    Guid BatchId,
    Guid ReminderRuleId,
    Guid EventId,
    long EventVersion,
    DateTimeOffset OriginalStartAtUtc,
    DateTimeOffset EffectiveStartAtUtc,
    DateTimeOffset ScheduledFireAtUtc,
    IReadOnlyList<Guid> RecipientUserIds);
