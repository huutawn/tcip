using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Infrastructure.Data;
using TCIP.Infrastructure.Messaging;

namespace TCIP.Infrastructure.Workers;

public sealed class AudienceResolverWorker(
    IServiceScopeFactory scopeFactory,
    IProducer<string, string> producer,
    IReminderDispatchValidator dispatchValidator,
    IAudienceRecipientResolver recipientResolver,
    TimeProvider timeProvider,
    IConfiguration configuration,
    ILogger<AudienceResolverWorker> logger) : BackgroundService
{
    private const int MaxBatchSize = 100;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeAsync(stoppingToken), stoppingToken);

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        var maxPollIntervalMs = Math.Max(
            30000,
            configuration.GetValue<int?>("Kafka:MaxPollIntervalMs") ?? 300000);

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = KafkaConfiguration.BootstrapServers(configuration),
            GroupId = KafkaConfiguration.AudienceResolverGroup(configuration),
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            MaxPollIntervalMs = maxPollIntervalMs
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        var topic = KafkaConfiguration.ReminderDueTopic(configuration);
        consumer.Subscribe(topic);

        logger.LogInformation("AudienceResolverWorker subscribed to topic {Topic}.", topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var messages = new List<ConsumeResult<string, string>>();
                for (var i = 0; i < MaxBatchSize; i++)
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(i == 0 ? 500 : 50));
                    if (consumeResult is null)
                        break;

                    messages.Add(consumeResult);
                }

                if (messages.Count == 0)
                {
                    await Task.Delay(50, cancellationToken);
                    continue;
                }

                await ProcessBatchAsync(consumer, messages, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AudienceResolverWorker encountered an error while consuming messages.");
                await Task.Delay(1000, cancellationToken);
            }
        }

        consumer.Close();
    }

    private async Task ProcessBatchAsync(
        IConsumer<string, string> consumer,
        IReadOnlyList<ConsumeResult<string, string>> messages,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            try
            {
                ReminderDueMessage? reminderDue;
                try
                {
                    reminderDue = JsonSerializer.Deserialize<ReminderDueMessage>(msg.Message.Value);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Malformed ReminderDueMessage at offset {Offset}. Skipping message.", msg.Offset);
                    consumer.StoreOffset(msg);
                    consumer.Commit(msg);
                    continue;
                }

                if (reminderDue is null)
                {
                    consumer.StoreOffset(msg);
                    consumer.Commit(msg);
                    continue;
                }

                await ResolveAndPublishBatchesAsync(reminderDue, cancellationToken);
                consumer.StoreOffset(msg);
                consumer.Commit(msg);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                RewindUnprocessedMessages(consumer, messages, i);
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error resolving audience for message {Offset} in partition {Partition}.", msg.Offset, msg.Partition);
                RewindUnprocessedMessages(consumer, messages, i);
                await Task.Delay(1000, cancellationToken);
                return;
            }
        }
    }

    public async Task ResolveAndPublishBatchesAsync(ReminderDueMessage message, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TcipDbContext>();

        var calendarEvent = await dbContext.Events
            .AsNoTracking()
            .Include(x => x.OccurrenceExceptions)
            .SingleOrDefaultAsync(x => x.Id == message.EventId, cancellationToken);

        var rule = await dbContext.ReminderRules
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == message.ReminderRuleId, cancellationToken);

        var validationResult = dispatchValidator.ValidateDispatch(
            calendarEvent,
            rule,
            message.EventId,
            message.ReminderRuleId,
            message.OriginalStartAtUtc,
            message.EffectiveStartAtUtc,
            message.ScheduledFireAtUtc,
            message.RepeatIndex);

        if (!validationResult.IsValid)
        {
            logger.LogInformation("Dropping ReminderDue for event {EventId}, rule {RuleId}: {Reason}", message.EventId, message.ReminderRuleId, validationResult.DropReason);
            return;
        }

        var batchTopic = KafkaConfiguration.NotificationBatchTopic(configuration);
        var resolvedAt = timeProvider.GetUtcNow();
        Guid? cursor = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            var recipientIds = await recipientResolver.GetRecipientsForEventAsync(
                message.EventId,
                resolvedAt,
                cursor,
                1001,
                cancellationToken);

            if (recipientIds.Count == 0)
            {
                break;
            }

            var hasMore = recipientIds.Count > 1000;
            var batchRecipients = recipientIds.Take(1000).ToList();

            var batch = new NotificationBatchMessage(
                Guid.NewGuid(),
                message.ReminderRuleId,
                message.EventId,
                calendarEvent!.Version,
                message.OriginalStartAtUtc,
                message.EffectiveStartAtUtc,
                message.ScheduledFireAtUtc,
                message.RepeatIndex,
                batchRecipients);

            await producer.ProduceAsync(
                batchTopic,
                new Message<string, string>
                {
                    Key = batch.BatchId.ToString("D"),
                    Value = JsonSerializer.Serialize(batch)
                },
                cancellationToken);

            if (!hasMore)
            {
                break;
            }

            cursor = batchRecipients[^1];
        }
    }

    private static void RewindUnprocessedMessages(
        IConsumer<string, string> consumer,
        IReadOnlyList<ConsumeResult<string, string>> messages,
        int firstUnprocessedIndex)
    {
        foreach (var partition in messages
                     .Skip(firstUnprocessedIndex)
                     .GroupBy(x => x.TopicPartition)
                     .Select(group => new TopicPartitionOffset(
                         group.Key,
                         group.Min(x => x.Offset))))
        {
            consumer.Seek(partition);
        }
    }
}
