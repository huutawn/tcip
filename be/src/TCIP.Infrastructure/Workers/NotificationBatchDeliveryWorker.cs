using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Infrastructure.Messaging;
using TCIP.Infrastructure.Services;

namespace TCIP.Infrastructure.Workers;

public sealed class NotificationBatchDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<NotificationBatchDeliveryWorker> logger) : BackgroundService
{
    private const int MaxBatchSize = 50;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeAsync(stoppingToken), stoppingToken);

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = KafkaConfiguration.BootstrapServers(configuration),
            GroupId = KafkaConfiguration.NotificationBatchDeliveryGroup(configuration),
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        var topic = KafkaConfiguration.NotificationBatchTopic(configuration);
        consumer.Subscribe(topic);

        logger.LogInformation("NotificationBatchDeliveryWorker subscribed to topic {Topic}.", topic);

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
                logger.LogError(ex, "NotificationBatchDeliveryWorker encountered an error while consuming messages.");
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
                var batchMessage = JsonSerializer.Deserialize<NotificationBatchMessage>(msg.Message.Value)
                    ?? throw new InvalidOperationException("Failed to deserialize NotificationBatchMessage.");

                using var scope = scopeFactory.CreateScope();
                var deliveryService = scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>();
                await deliveryService.DeliverBatchAsync(batchMessage, cancellationToken);

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
                logger.LogError(ex, "Error delivering notification batch for message {Offset} in partition {Partition}.", msg.Offset, msg.Partition);
                RewindUnprocessedMessages(consumer, messages, i);
                await Task.Delay(1000, cancellationToken);
                return;
            }
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
