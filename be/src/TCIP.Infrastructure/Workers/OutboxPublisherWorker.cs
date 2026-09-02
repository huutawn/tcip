using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Workers;

public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    IProducer<string, string> producer,
    TimeProvider timeProvider,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan PublishingLease = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishDueMessagesAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in OutboxPublisherWorker.");
            }
        }
    }

    private async Task PublishDueMessagesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<OutboxMessage> messages;
        try
        {
            messages = await ClaimMessagesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Claiming outbox messages failed.");
            return;
        }

        if (messages.Count == 0)
        {
            return;
        }

        var publishedIds = new List<Guid>();
        var failedMessages = new List<(OutboxMessage Message, Exception Exception)>();

        var publishTasks = messages.Select(async message =>
        {
            try
            {
                await producer.ProduceAsync(
                    message.Topic,
                    new Message<string, string>
                    {
                        Key = message.ReminderRuleId.ToString("D"),
                        Value = message.Payload
                    },
                    cancellationToken);

                lock (publishedIds)
                {
                    publishedIds.Add(message.Id);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Publishing outbox message {OutboxMessageId} failed.", message.Id);
                lock (failedMessages)
                {
                    failedMessages.Add((message, exception));
                }
            }
        });

        await Task.WhenAll(publishTasks);

        if (publishedIds.Count > 0)
        {
            await MarkPublishedBatchAsync(publishedIds, cancellationToken);
        }

        foreach (var (failedMsg, ex) in failedMessages)
        {
            await ScheduleRetryAsync(failedMsg, ex, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<OutboxMessage>> ClaimMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TcipDbContext>();
        var now = timeProvider.GetUtcNow();
        var leaseExpiresAt = now.Add(PublishingLease);

        OutboxMessage[] messages;
        if (!dbContext.Database.IsRelational())
        {
            messages = await dbContext.OutboxMessages
                .Where(o => (o.Status == OutboxMessageStatus.Pending && o.NextAttemptAtUtc <= now) ||
                            (o.Status == OutboxMessageStatus.Publishing && o.PublishingLeaseExpiresAtUtc <= now))
                .OrderBy(o => o.NextAttemptAtUtc)
                .Take(BatchSize)
                .ToArrayAsync(cancellationToken);
        }
        else
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            messages = await dbContext.OutboxMessages
                .FromSqlInterpolated($"""
                    SELECT o.* FROM outbox_messages AS o
                    WHERE (o.status = 'Pending' AND o.next_attempt_at_utc <= {now})
                        OR (o.status = 'Publishing' AND o.publishing_lease_expires_at_utc <= {now})
                    ORDER BY o.next_attempt_at_utc
                    LIMIT {BatchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToArrayAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        foreach (var message in messages)
        {
            message.Status = OutboxMessageStatus.Publishing;
            message.PublishingLeaseExpiresAtUtc = leaseExpiresAt;
            message.AttemptCount++;
            message.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return messages.Select(x => new OutboxMessage
        {
            Id = x.Id,
            ReminderRuleId = x.ReminderRuleId,
            Topic = x.Topic,
            Payload = x.Payload,
            AttemptCount = x.AttemptCount
        }).ToArray();
    }

    private async Task MarkPublishedBatchAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TcipDbContext>();
        var now = timeProvider.GetUtcNow();

        if (!dbContext.Database.IsRelational())
        {
            var msgs = await dbContext.OutboxMessages.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
            foreach (var m in msgs)
            {
                m.Status = OutboxMessageStatus.Published;
                m.PublishedAtUtc = now;
                m.PublishingLeaseExpiresAtUtc = null;
                m.UpdatedAtUtc = now;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await dbContext.OutboxMessages
            .Where(x => ids.Contains(x.Id) && x.Status == OutboxMessageStatus.Publishing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, OutboxMessageStatus.Published)
                .SetProperty(x => x.PublishedAtUtc, now)
                .SetProperty(x => x.PublishingLeaseExpiresAtUtc, (DateTimeOffset?)null)
                .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
    }

    private async Task ScheduleRetryAsync(OutboxMessage message, Exception exception, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TcipDbContext>();
        var now = timeProvider.GetUtcNow();
        var seconds = Math.Min(300, Math.Pow(2, Math.Min(message.AttemptCount, 8)));

        if (!dbContext.Database.IsRelational())
        {
            var msg = await dbContext.OutboxMessages.FindAsync([message.Id], cancellationToken);
            if (msg != null)
            {
                msg.Status = OutboxMessageStatus.Pending;
                msg.NextAttemptAtUtc = now.AddSeconds(seconds);
                msg.PublishingLeaseExpiresAtUtc = null;
                msg.LastError = exception.Message[..Math.Min(exception.Message.Length, 2000)];
                msg.UpdatedAtUtc = now;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return;
        }

        await dbContext.OutboxMessages
            .Where(x => x.Id == message.Id && x.Status == OutboxMessageStatus.Publishing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, OutboxMessageStatus.Pending)
                .SetProperty(x => x.NextAttemptAtUtc, now.AddSeconds(seconds))
                .SetProperty(x => x.PublishingLeaseExpiresAtUtc, (DateTimeOffset?)null)
                .SetProperty(x => x.LastError, exception.Message[..Math.Min(exception.Message.Length, 2000)])
                .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
    }
}
