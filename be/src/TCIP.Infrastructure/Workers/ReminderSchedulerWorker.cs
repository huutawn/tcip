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
            var initialFire = effStart.AddMinutes(-rule.RemindBeforeMinutes);
            var maxLateness = TimeSpan.FromMinutes(rule.MaxLatenessMinutes);
            var lateness = now - schedule.NextFireAtUtc;
            var isMisfired = lateness > maxLateness;

            switch (rule.MisfirePolicy)
            {
                case MisfirePolicy.Skip:
                    if (isMisfired)
                    {
                        AdvanceToNextFutureFireOrOccurrence(rule, schedule, calendarEvent, recurrenceEngine, effStart, initialFire, now);
                    }
                    else
                    {
                        EmitOutbox(dbContext, rule, schedule, calendarEvent, effStart, schedule.NextFireAtUtc, schedule.RepeatIndex, topic, now);
                        AdvanceNormal(rule, schedule, calendarEvent, recurrenceEngine, effStart, initialFire, now);
                    }
                    break;

                case MisfirePolicy.FireOnceNow:
                    if (isMisfired)
                    {
                        var latestMissedFire = FindLatestMissedFireWithinLateness(rule, effStart, initialFire, schedule.RepeatIndex, now, maxLateness);
                        if (latestMissedFire.HasValue)
                        {
                            EmitOutbox(dbContext, rule, schedule, calendarEvent, effStart, latestMissedFire.Value.FireTime, latestMissedFire.Value.RepeatIndex, topic, now);
                        }
                        AdvanceToNextFutureFireOrOccurrence(rule, schedule, calendarEvent, recurrenceEngine, effStart, initialFire, now);
                    }
                    else
                    {
                        EmitOutbox(dbContext, rule, schedule, calendarEvent, effStart, schedule.NextFireAtUtc, schedule.RepeatIndex, topic, now);
                        AdvanceNormal(rule, schedule, calendarEvent, recurrenceEngine, effStart, initialFire, now);
                    }
                    break;

                case MisfirePolicy.CatchUp:
                    if (isMisfired)
                    {
                        var firstValidFire = FindFirstFireWithinLateness(rule, effStart, initialFire, schedule.RepeatIndex, now, maxLateness);
                        if (firstValidFire.HasValue)
                        {
                            schedule.RepeatIndex = firstValidFire.Value.RepeatIndex;
                            schedule.NextFireAtUtc = firstValidFire.Value.FireTime;
                            schedule.UpdatedAtUtc = now;

                            EmitOutbox(dbContext, rule, schedule, calendarEvent, effStart, schedule.NextFireAtUtc, schedule.RepeatIndex, topic, now);
                            AdvanceNormal(rule, schedule, calendarEvent, recurrenceEngine, effStart, initialFire, now);
                        }
                        else
                        {
                            AdvanceToNextFutureFireOrOccurrence(rule, schedule, calendarEvent, recurrenceEngine, effStart, initialFire, now);
                        }
                    }
                    else
                    {
                        EmitOutbox(dbContext, rule, schedule, calendarEvent, effStart, schedule.NextFireAtUtc, schedule.RepeatIndex, topic, now);
                        AdvanceNormal(rule, schedule, calendarEvent, recurrenceEngine, effStart, initialFire, now);
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
        int repeatIndex,
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
            scheduledFire,
            repeatIndex);

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = message.MessageId,
            ReminderRuleId = rule.Id,
            OccurrenceStartAtUtc = schedule.OccurrenceStartAtUtc,
            ScheduledFireAtUtc = scheduledFire,
            RepeatIndex = repeatIndex,
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

    private static (DateTimeOffset FireTime, int RepeatIndex)? FindLatestMissedFireWithinLateness(
        ReminderRule rule,
        DateTimeOffset effStart,
        DateTimeOffset initialFire,
        int startIndex,
        DateTimeOffset now,
        TimeSpan maxLateness)
    {
        (DateTimeOffset FireTime, int RepeatIndex)? latest = null;
        for (var idx = startIndex; idx <= rule.RepeatCount; idx++)
        {
            var fire = idx == 0 ? initialFire : initialFire.AddMinutes(idx * (rule.RepeatEveryMinutes ?? 0));
            if (fire <= now && fire >= now - maxLateness && fire < effStart)
            {
                latest = (fire, idx);
            }
        }
        return latest;
    }

    private static (DateTimeOffset FireTime, int RepeatIndex)? FindFirstFireWithinLateness(
        ReminderRule rule,
        DateTimeOffset effStart,
        DateTimeOffset initialFire,
        int startIndex,
        DateTimeOffset now,
        TimeSpan maxLateness)
    {
        for (var idx = startIndex; idx <= rule.RepeatCount; idx++)
        {
            var fire = idx == 0 ? initialFire : initialFire.AddMinutes(idx * (rule.RepeatEveryMinutes ?? 0));
            if (fire >= now - maxLateness && fire < effStart)
            {
                return (fire, idx);
            }
        }
        return null;
    }

    private static void AdvanceNormal(
        ReminderRule rule,
        ReminderSchedule schedule,
        Event calendarEvent,
        IRecurrenceEngine recurrenceEngine,
        DateTimeOffset effStart,
        DateTimeOffset initialFire,
        DateTimeOffset now)
    {
        if (rule.RepeatCount > 0 && schedule.RepeatIndex < rule.RepeatCount && rule.RepeatEveryMinutes.HasValue)
        {
            var nextIndex = schedule.RepeatIndex + 1;
            var nextFire = initialFire.AddMinutes(nextIndex * rule.RepeatEveryMinutes.Value);
            if (nextFire < effStart)
            {
                schedule.RepeatIndex = nextIndex;
                schedule.NextFireAtUtc = nextFire;
                schedule.EventVersion = calendarEvent.Version;
                schedule.UpdatedAtUtc = now;
                return;
            }
        }

        AdvanceToNextOccurrence(rule, schedule, calendarEvent, recurrenceEngine, now);
    }

    private static void AdvanceToNextFutureFireOrOccurrence(
        ReminderRule rule,
        ReminderSchedule schedule,
        Event calendarEvent,
        IRecurrenceEngine recurrenceEngine,
        DateTimeOffset effStart,
        DateTimeOffset initialFire,
        DateTimeOffset now)
    {
        if (rule.RepeatCount > 0 && rule.RepeatEveryMinutes.HasValue)
        {
            for (var idx = schedule.RepeatIndex + 1; idx <= rule.RepeatCount; idx++)
            {
                var fire = initialFire.AddMinutes(idx * rule.RepeatEveryMinutes.Value);
                if (fire < effStart && fire > now)
                {
                    schedule.RepeatIndex = idx;
                    schedule.NextFireAtUtc = fire;
                    schedule.EventVersion = calendarEvent.Version;
                    schedule.UpdatedAtUtc = now;
                    return;
                }
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
        schedule.RepeatIndex = 0;
        schedule.EventVersion = calendarEvent.Version;
        schedule.Status = ReminderScheduleStatus.Active;
        schedule.UpdatedAtUtc = now;
    }
}
