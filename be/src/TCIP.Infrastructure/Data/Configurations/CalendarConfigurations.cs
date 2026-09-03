using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCIP.Business.Modules.Calendar.Domain.Entities;

namespace TCIP.Infrastructure.Data.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events", t =>
        {
            t.HasCheckConstraint("ck_events_end_after_start", "end_at_utc IS NULL OR end_at_utc >= start_at_utc");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedById).HasColumnName("created_by").IsRequired();
        builder.Property(x => x.StartAtUtc).HasColumnName("start_at_utc").IsRequired();
        builder.Property(x => x.EndAtUtc).HasColumnName("end_at_utc");
        builder.Property(x => x.TimeZoneId).HasColumnName("timezone").HasMaxLength(128).IsRequired();
        builder.Property(x => x.RecurrenceRule).HasColumnName("recurrence_rule");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.Status, x.StartAtUtc });
    }
}

public sealed class EventAudienceConfiguration : IEntityTypeConfiguration<EventAudience>
{
    public void Configure(EntityTypeBuilder<EventAudience> builder)
    {
        builder.ToTable("event_audiences");
        builder.HasKey(x => new { x.EventId, x.PrincipalId });
        builder.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(x => x.PrincipalId).HasColumnName("principal_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasOne(x => x.Event).WithMany(x => x.Audiences).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Principal).WithMany().HasForeignKey(x => x.PrincipalId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.PrincipalId, x.Status, x.EventId });
    }
}

public sealed class EventTranslationConfiguration : IEntityTypeConfiguration<EventTranslation>
{
    public void Configure(EntityTypeBuilder<EventTranslation> builder)
    {
        builder.ToTable("event_translations");
        builder.HasKey(x => new { x.EventId, x.Language });
        builder.Property(x => x.EventId).HasColumnName("event_id");
        builder.Property(x => x.Language).HasColumnName("language").HasMaxLength(16);
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");

        builder.HasOne(x => x.Event).WithMany(x => x.Translations).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EventOccurrenceExceptionConfiguration : IEntityTypeConfiguration<EventOccurrenceException>
{
    public void Configure(EntityTypeBuilder<EventOccurrenceException> builder)
    {
        builder.ToTable("event_occurrence_exceptions", t =>
        {
            t.HasCheckConstraint("ck_event_occurrence_exceptions_cancelled", "(is_cancelled = TRUE AND start_at_utc IS NULL AND end_at_utc IS NULL) OR (is_cancelled = FALSE AND (start_at_utc IS NOT NULL OR end_at_utc IS NOT NULL))");
            t.HasCheckConstraint("ck_event_occurrence_exceptions_end_after_start", "is_cancelled = TRUE OR end_at_utc IS NULL OR start_at_utc IS NULL OR end_at_utc > start_at_utc");
        });
        builder.HasKey(x => new { x.EventId, x.OriginalStartAtUtc });
        builder.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(x => x.OriginalStartAtUtc).HasColumnName("original_start_at_utc").IsRequired();
        builder.Property(x => x.IsCancelled).HasColumnName("is_cancelled").IsRequired();
        builder.Property(x => x.StartAtUtc).HasColumnName("start_at_utc");
        builder.Property(x => x.EndAtUtc).HasColumnName("end_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne(x => x.Event).WithMany(x => x.OccurrenceExceptions).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ReminderRuleConfiguration : IEntityTypeConfiguration<ReminderRule>
{
    public void Configure(EntityTypeBuilder<ReminderRule> builder)
    {
        builder.ToTable("reminder_rules", t =>
        {
            t.HasCheckConstraint("ck_reminder_rules_repeat_interval", "repeat_every_minutes IS NULL OR repeat_every_minutes > 0");
            t.HasCheckConstraint("ck_reminder_rules_max_lateness", "max_lateness_minutes >= 0");
            t.HasCheckConstraint("ck_reminder_rules_remind_before", "remind_before_minutes >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(x => x.RemindBeforeMinutes).HasColumnName("remind_before_minutes").IsRequired();
        builder.Property(x => x.RepeatEveryMinutes).HasColumnName("repeat_every_minutes");
        builder.Property(x => x.MisfirePolicy).HasColumnName("misfire_policy").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.MaxLatenessMinutes).HasColumnName("max_lateness_minutes").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne(x => x.Event).WithMany(x => x.ReminderRules).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.EventId, x.Status });
    }
}

public sealed class ReminderScheduleConfiguration : IEntityTypeConfiguration<ReminderSchedule>
{
    public void Configure(EntityTypeBuilder<ReminderSchedule> builder)
    {
        builder.ToTable("reminder_schedules");
        builder.HasKey(x => x.ReminderRuleId);
        builder.Property(x => x.ReminderRuleId).HasColumnName("reminder_rule_id");
        builder.Property(x => x.OccurrenceStartAtUtc).HasColumnName("occurrence_start_at_utc").IsRequired();
        builder.Property(x => x.NextFireAtUtc).HasColumnName("next_fire_at_utc").IsRequired();
        builder.Property(x => x.EventVersion).HasColumnName("event_version").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne(x => x.ReminderRule).WithOne(x => x.Schedule).HasForeignKey<ReminderSchedule>(x => x.ReminderRuleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.Status, x.NextFireAtUtc });
    }
}

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ReminderRuleId).HasColumnName("reminder_rule_id").IsRequired();
        builder.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id").IsRequired();
        builder.Property(x => x.OriginalStartAtUtc).HasColumnName("original_start_at_utc").IsRequired();
        builder.Property(x => x.EffectiveStartAtUtc).HasColumnName("effective_start_at_utc").IsRequired();
        builder.Property(x => x.ScheduledFireAtUtc).HasColumnName("scheduled_fire_at_utc").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.SentAtUtc).HasColumnName("sent_at_utc").IsRequired();
        builder.Property(x => x.ReadAtUtc).HasColumnName("read_at_utc");

        builder.HasOne(x => x.ReminderRule).WithMany().HasForeignKey(x => x.ReminderRuleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.RecipientUser).WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ReminderRuleId, x.OriginalStartAtUtc, x.ScheduledFireAtUtc, x.RecipientUserId }).IsUnique();
        builder.HasIndex(x => new { x.RecipientUserId, x.ReadAtUtc, x.SentAtUtc });
    }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", t =>
        {
            t.HasCheckConstraint("ck_outbox_messages_attempt_count", "attempt_count >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ReminderRuleId).HasColumnName("reminder_rule_id").IsRequired();
        builder.Property(x => x.OccurrenceStartAtUtc).HasColumnName("occurrence_start_at_utc").IsRequired();
        builder.Property(x => x.ScheduledFireAtUtc).HasColumnName("scheduled_fire_at_utc").IsRequired();
        builder.Property(x => x.EventVersion).HasColumnName("event_version").IsRequired();
        builder.Property(x => x.Topic).HasColumnName("topic").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(x => x.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc").IsRequired();
        builder.Property(x => x.PublishingLeaseExpiresAtUtc).HasColumnName("publishing_lease_expires_at_utc");
        builder.Property(x => x.LastError).HasColumnName("last_error");
        builder.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
        builder.HasIndex(x => new { x.ReminderRuleId, x.OccurrenceStartAtUtc, x.ScheduledFireAtUtc, x.EventVersion }).IsUnique();
    }
}
