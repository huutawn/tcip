using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Entities;

namespace TCIP.Business.Modules.Calendar.Domain.Entities;

public sealed class Event
{
    public Guid Id { get; set; }
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTimeOffset StartAtUtc { get; set; }
    public DateTimeOffset? EndAtUtc { get; set; }
    public string TimeZoneId { get; set; } = null!;
    public string? RecurrenceRule { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Active;
    public long Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<EventAudience> Audiences { get; } = new List<EventAudience>();
    public ICollection<EventTranslation> Translations { get; } = new List<EventTranslation>();
    public ICollection<ReminderRule> ReminderRules { get; } = new List<ReminderRule>();
    public ICollection<EventOccurrenceException> OccurrenceExceptions { get; } = new List<EventOccurrenceException>();
}

public sealed class EventAudience
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid PrincipalId { get; set; }
    public Principal Principal { get; set; } = null!;
    public EventAudienceStatus Status { get; set; } = EventAudienceStatus.Active;
}

public sealed class EventTranslation
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string Language { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
}

public sealed class EventOccurrenceException
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public DateTimeOffset OriginalStartAtUtc { get; set; }
    public bool IsCancelled { get; set; }
    public DateTimeOffset? StartAtUtc { get; set; }
    public DateTimeOffset? EndAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ReminderRule
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public int RemindBeforeMinutes { get; set; }
    public int? RepeatEveryMinutes { get; set; }
    public MisfirePolicy MisfirePolicy { get; set; } = MisfirePolicy.FireOnceNow;
    public int MaxLatenessMinutes { get; set; } = 15;
    public ReminderRuleStatus Status { get; set; } = ReminderRuleStatus.Active;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ReminderSchedule? Schedule { get; set; }
}

public sealed class ReminderSchedule
{
    public Guid ReminderRuleId { get; set; }
    public ReminderRule ReminderRule { get; set; } = null!;
    public DateTimeOffset OccurrenceStartAtUtc { get; set; }
    public DateTimeOffset NextFireAtUtc { get; set; }
    public long EventVersion { get; set; } = 1;
    public ReminderScheduleStatus Status { get; set; } = ReminderScheduleStatus.Active;
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid ReminderRuleId { get; set; }
    public ReminderRule ReminderRule { get; set; } = null!;
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid RecipientUserId { get; set; }
    public User RecipientUser { get; set; } = null!;
    public DateTimeOffset OriginalStartAtUtc { get; set; }
    public DateTimeOffset EffectiveStartAtUtc { get; set; }
    public DateTimeOffset ScheduledFireAtUtc { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTimeOffset SentAtUtc { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid ReminderRuleId { get; set; }
    public DateTimeOffset OccurrenceStartAtUtc { get; set; }
    public DateTimeOffset ScheduledFireAtUtc { get; set; }
    public long EventVersion { get; set; }
    public string Topic { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTimeOffset NextAttemptAtUtc { get; set; }
    public DateTimeOffset? PublishingLeaseExpiresAtUtc { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
