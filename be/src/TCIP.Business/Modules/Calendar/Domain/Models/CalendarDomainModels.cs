using TCIP.Business.Modules.Calendar.Domain.Entities;

namespace TCIP.Business.Modules.Calendar.Domain.Models;

public sealed record OccurrenceDetails(
    DateTimeOffset OriginalStartAtUtc,
    DateTimeOffset StartAtUtc,
    DateTimeOffset? EndAtUtc);

public sealed record DispatchValidationResult(
    bool IsValid,
    string? DropReason,
    OccurrenceDetails? Occurrence)
{
    public static DispatchValidationResult Valid(OccurrenceDetails occurrence) => new(true, null, occurrence);
    public static DispatchValidationResult Drop(string reason) => new(false, reason, null);
}

public static class CalendarKafkaTopics
{
    public const string ReminderDueTopicV2 = "calendar.reminder-due.v2";
    public const string NotificationBatchTopicV1 = "calendar.notification-batch.v1";
    public const string AudienceResolverGroupV2 = "calendar.audience-resolver.v2";
    public const string NotificationBatchDeliveryGroupV1 = "calendar.notification-batch-delivery.v1";
}
