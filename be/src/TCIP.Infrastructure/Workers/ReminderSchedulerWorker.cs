using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Infrastructure.Data;
using TCIP.Infrastructure.Messaging;

namespace TCIP.Infrastructure.Workers;

public sealed class ReminderSchedulerWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IRecurrenceEngine recurrenceEngine,
    IReminderSchedulePlanner reminderSchedulePlanner,
    TimeProvider timeProvider,
    ILogger<ReminderSchedulerWorker> logger) : BackgroundService
{
    private const int BatchSize = 1000;
    private readonly int maxBatchesPerRun = Math.Max(
        1,
        configuration.GetValue<int?>("ReminderScheduler:MaxBatchesPerRun") ?? 10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScheduleDueRemindersAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in ReminderSchedulerWorker.");
            }
        }
    }

    private async Task ScheduleDueRemindersAsync(CancellationToken cancellationToken)
    {
        for (var batchIndex = 0; batchIndex < maxBatchesPerRun; batchIndex++)
        {
            var processed = await ScheduleBatchAsync(cancellationToken);
            if (processed < BatchSize)
            {
                break;
            }
        }
    }

    public async Task<int> ScheduleBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TcipDbContext>();
        var now = timeProvider.GetUtcNow();
        var topic = KafkaConfiguration.ReminderDueTopic(configuration);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var schedules = await dbContext.ReminderSchedules
            .FromSqlInterpolated($"""
                SELECT s.* FROM reminder_schedules AS s
                JOIN reminder_rules AS r ON s.reminder_rule_id = r.id
                WHERE s.status = 'Active' AND s.next_fire_at_utc <= {now}
                ORDER BY s.next_fire_at_utc
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .Include(x => x.ReminderRule)
                .ThenInclude(x => x.Event)
                    .ThenInclude(x => x.OccurrenceExceptions)
            .ToListAsync(cancellationToken);

        if (schedules.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return 0;
        }

        foreach (var schedule in schedules)
        {
            var rule = schedule.ReminderRule;
            var calendarEvent = rule.Event;

            if (calendarEvent.Status != EventStatus.Active || rule.Status != ReminderRuleStatus.Active)
            {
                schedule.Status = ReminderScheduleStatus.Cancelled;
                schedule.UpdatedAtUtc = now;
                continue;
            }

            if (schedule.EventVersion != calendarEvent.Version)
            {
                reminderSchedulePlanner.ReprojectSchedule(rule, calendarEvent, now);
                if (schedule.NextFireAtUtc > now || schedule.Status != ReminderScheduleStatus.Active)
                {
                    continue;
                }
            }

            var resolvedOccurrence = recurrenceEngine.ResolveOriginalOccurrence(calendarEvent, schedule.OccurrenceStartAtUtc);
            if (resolvedOccurrence is null)
            {
                AdvanceToNextOccurrence(rule, schedule, calendarEvent, recurrenceEngine, now);
                continue;
            }

            var effStart = resolvedOccurrence.StartAtUtc;
            var maxLateness = TimeSpan.FromMinutes(rule.MaxLatenessMinutes);
            var lateness = now - schedule.NextFireAtUtc;
            var isMisfired = lateness > maxLateness;

            switch (rule.MisfirePolicy)
            {
                case MisfirePolicy.Skip:
                    if (isMisfired)
                    {
                        AdvanceToNextFutureFireOrOccurrence(rule, schedule, calendarEvent, recurrenceEngine, effStart, now);
                    }
                    else
                    {
                        EmitOutbox(dbContext, rule, schedule, calendarEvent, effStart, schedule.NextFireAtUtc, topic, now);
                        AdvanceNormal(rule, schedule, calendarEvent, recurrenceEngine, effStart, now);
                    }
                    break;

                case MisfirePolicy.FireOnceNow:
                    if (isMisfired)
                    {
                        var latestMissedFire = FindLatestMissedFireWithinLateness(rule, effStart, schedule.NextFireAtUtc, now, maxLateness);
                        if (latestMissedFire.HasValue)
                        {
                            EmitOutbox(dbContext, rule, schedule, calendarEvent, effStart, latestMissedFire.Value, topic, now);
                        }
                        AdvanceToNextFutureFireOrOccurrence(rule, schedule, calendarEvent, recurrenceEngine, effStart, now);
                    }
                    else
                    {
                        EmitOutbox(dbContext, rule, schedule, calendarEvent, effStart, schedule.NextFireAtUtc, topic, now);
                        AdvanceNormal(rule, schedule, calendarEvent, recurrenceEngine, effStart, now);
                    }
                    break;

                case MisfirePolicy.CatchUp:
                    if (isMisfired)
                    {
                        var firstValidFire = FindFirstFireWithinLateness(rule, effStart, schedule.NextFireAtUtc, now, maxLateness);
                        if (firstValidFire.HasValue)
                        {
                            schedule.NextFireAtUtc = firstValidFire.Value;
                            schedule.UpdatedAtUtc = now;

                            EmitOutbox(dbContext, rule, schedule, calendarEvent, effStart, schedule.NextFireAtUtc, topic, now);
                            AdvanceNormal(rule, schedule, calendarEvent, recurrenceEngine, effStart, now);
                        }
                        else
                        {
                            AdvanceToNextFutureFireOrOccurrence(rule, schedule, calendarEvent, recurrenceEngine, effStart, now);
                        }
                    }
                    else
                    {
                        EmitOutbox(dbContext, rule, schedule, calendarEvent, effStart, schedule.NextFireAtUtc, topic, now);
                        AdvanceNormal(rule, schedule, calendarEvent, recurrenceEngine, effStart, now);
                    }
                    break;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return schedules.Count;
    }

    private static void EmitOutbox(
        TcipDbContext dbContext,
        ReminderRule rule,
        ReminderSchedule schedule,
        Event calendarEvent,
        DateTimeOffset effStart,
        DateTimeOffset scheduledFire,
        string topic,
        DateTimeOffset now)
    {
        var message = new ReminderDueMessage(
            Guid.NewGuid(),
            rule.Id,
            calendarEvent.Id,
            calendarEvent.Version,
            schedule.OccurrenceStartAtUtc,
            effStart,
            scheduledFire);

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = message.MessageId,
            ReminderRuleId = rule.Id,
            OccurrenceStartAtUtc = schedule.OccurrenceStartAtUtc,
            ScheduledFireAtUtc = scheduledFire,
            EventVersion = calendarEvent.Version,
            Topic = topic,
            Payload = JsonSerializer.Serialize(message),
            Status = OutboxMessageStatus.Pending,
            AttemptCount = 0,
            NextAttemptAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }

    private static DateTimeOffset? FindLatestMissedFireWithinLateness(
        ReminderRule rule,
        DateTimeOffset effStart,
        DateTimeOffset nextFire,
        DateTimeOffset now,
        TimeSpan maxLateness)
    {
        if (nextFire >= now - maxLateness && nextFire <= now && nextFire < effStart)
            return LastDueRepeat(rule, nextFire, effStart, now);

        return FirstValidRepeat(rule, nextFire, effStart, now - maxLateness);
    }

    private static DateTimeOffset? FindFirstFireWithinLateness(
        ReminderRule rule,
        DateTimeOffset effStart,
        DateTimeOffset nextFire,
        DateTimeOffset now,
        TimeSpan maxLateness)
    {
        return FirstValidRepeat(rule, nextFire, effStart, now - maxLateness);
    }

    private static void AdvanceNormal(
        ReminderRule rule,
        ReminderSchedule schedule,
        Event calendarEvent,
        IRecurrenceEngine recurrenceEngine,
        DateTimeOffset effStart,
        DateTimeOffset now)
    {
        if (TryGetNextRepeatFire(rule, schedule.NextFireAtUtc, effStart, out var nextFire))
        {
            schedule.NextFireAtUtc = nextFire;
            schedule.EventVersion = calendarEvent.Version;
            schedule.UpdatedAtUtc = now;
            return;
        }

        AdvanceToNextOccurrence(rule, schedule, calendarEvent, recurrenceEngine, now);
    }

    private static void AdvanceToNextFutureFireOrOccurrence(
        ReminderRule rule,
        ReminderSchedule schedule,
        Event calendarEvent,
        IRecurrenceEngine recurrenceEngine,
        DateTimeOffset effStart,
        DateTimeOffset now)
    {
        if (TryGetNextRepeatFire(rule, schedule.NextFireAtUtc, effStart, out var nextFire))
        {
            var interval = TimeSpan.FromMinutes(rule.RepeatEveryMinutes!.Value);
            var skipped = Math.Max(0, (long)Math.Floor((now - nextFire).Ticks / (double)interval.Ticks) + 1);
            nextFire = nextFire.AddTicks(interval.Ticks * skipped);
            if (nextFire < effStart)
            {
                schedule.NextFireAtUtc = nextFire;
                schedule.EventVersion = calendarEvent.Version;
                schedule.UpdatedAtUtc = now;
                return;
            }
        }

        AdvanceToNextOccurrence(rule, schedule, calendarEvent, recurrenceEngine, now);
    }

    private static void AdvanceToNextOccurrence(
        ReminderRule rule,
        ReminderSchedule schedule,
        Event calendarEvent,
        IRecurrenceEngine recurrenceEngine,
        DateTimeOffset now)
    {
        var exceptions = calendarEvent.OccurrenceExceptions.ToList();
        var nextOccurrence = recurrenceEngine.GetNextOccurrence(
            calendarEvent.RecurrenceRule,
            calendarEvent.StartAtUtc,
            calendarEvent.EndAtUtc,
            calendarEvent.TimeZoneId,
            schedule.OccurrenceStartAtUtc,
            exceptions);

        if (nextOccurrence is null)
        {
            schedule.Status = ReminderScheduleStatus.Completed;
            schedule.EventVersion = calendarEvent.Version;
            schedule.UpdatedAtUtc = now;
            return;
        }

        var nextEffStart = nextOccurrence.StartAtUtc;
        schedule.OccurrenceStartAtUtc = nextOccurrence.OriginalStartAtUtc;
        schedule.NextFireAtUtc = nextEffStart.AddMinutes(-rule.RemindBeforeMinutes);
        schedule.EventVersion = calendarEvent.Version;
        schedule.Status = ReminderScheduleStatus.Active;
        schedule.UpdatedAtUtc = now;
    }

    private static bool TryGetNextRepeatFire(
        ReminderRule rule,
        DateTimeOffset currentFire,
        DateTimeOffset effectiveStart,
        out DateTimeOffset nextFire)
    {
        nextFire = default;
        if (!rule.RepeatEveryMinutes.HasValue)
            return false;

        nextFire = currentFire.AddMinutes(rule.RepeatEveryMinutes.Value);
        return nextFire < effectiveStart;
    }

    private static DateTimeOffset? FirstValidRepeat(
        ReminderRule rule,
        DateTimeOffset nextFire,
        DateTimeOffset effectiveStart,
        DateTimeOffset minimumFire)
    {
        if (nextFire >= minimumFire && nextFire < effectiveStart)
            return nextFire;

        if (!rule.RepeatEveryMinutes.HasValue)
            return null;

        var interval = TimeSpan.FromMinutes(rule.RepeatEveryMinutes.Value);
        var steps = Math.Max(0, (long)Math.Ceiling((minimumFire - nextFire).Ticks / (double)interval.Ticks));
        var candidate = nextFire.AddTicks(interval.Ticks * steps);
        return candidate < effectiveStart ? candidate : null;
    }

    private static DateTimeOffset LastDueRepeat(
        ReminderRule rule,
        DateTimeOffset nextFire,
        DateTimeOffset effectiveStart,
        DateTimeOffset now)
    {
        if (!rule.RepeatEveryMinutes.HasValue)
            return nextFire;

        var interval = TimeSpan.FromMinutes(rule.RepeatEveryMinutes.Value);
        var lastPossible = now < effectiveStart ? now : effectiveStart.AddTicks(-1);
        var steps = Math.Max(0, (long)Math.Floor((lastPossible - nextFire).Ticks / (double)interval.Ticks));
        return nextFire.AddTicks(interval.Ticks * steps);
    }
}
