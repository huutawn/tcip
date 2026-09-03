using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Workers;

public sealed class CleanUpWorkder(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IConfiguration configuration,
    ILogger<CleanUpWorkder> logger) : BackgroundService
{
    public const int DefaultRetentionDays = 15;
    public const int DefaultIntervalHours = 24;
    public const int BatchSize = 1000;

    private readonly TimeSpan interval = TimeSpan.FromHours(Math.Max(
        1,
        configuration.GetValue<int?>("CleanUp:IntervalHours") ?? DefaultIntervalHours));

    private readonly int retentionDays = Math.Max(
        1,
        configuration.GetValue<int?>("CleanUp:RetentionDays") ?? DefaultRetentionDays);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOutdatedOutboxMessagesAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in CleanUpWorkder.");
            }
        }
    }

    public async Task<int> CleanupOutdatedOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var cutoff = now.AddDays(-retentionDays);
        var totalDeleted = 0;

        logger.LogInformation("Starting outbox cleanup for messages older than {Cutoff} (retention: {Days} days).", cutoff, retentionDays);

        while (!cancellationToken.IsCancellationRequested)
        {
            var deletedCount = await CleanupBatchAsync(cutoff, cancellationToken);
            totalDeleted += deletedCount;

            if (deletedCount < BatchSize)
            {
                break;
            }
        }

        if (totalDeleted > 0)
        {
            logger.LogInformation("Completed outbox cleanup. Deleted {TotalDeleted} messages older than {Cutoff}.", totalDeleted, cutoff);
        }
        else
        {
            logger.LogDebug("Outbox cleanup completed. No expired messages to delete.");
        }

        return totalDeleted;
    }

    private async Task<int> CleanupBatchAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TcipDbContext>();

        if (!dbContext.Database.IsRelational())
        {
            var ids = await dbContext.OutboxMessages
                .Where(x => x.Status == OutboxMessageStatus.Published &&
                            (x.PublishedAtUtc ?? x.CreatedAtUtc) <= cutoff)
                .OrderBy(x => x.PublishedAtUtc ?? x.CreatedAtUtc)
                .Take(BatchSize)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (ids.Count == 0)
            {
                return 0;
            }

            var toDelete = await dbContext.OutboxMessages
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            dbContext.OutboxMessages.RemoveRange(toDelete);
            await dbContext.SaveChangesAsync(cancellationToken);
            return ids.Count;
        }

        return await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM outbox_messages
            WHERE id IN (
                SELECT id FROM outbox_messages
                WHERE status = 'Published'
                  AND COALESCE(published_at_utc, created_at_utc) <= {cutoff}
                ORDER BY COALESCE(published_at_utc, created_at_utc)
                LIMIT {BatchSize}
            )
            """, cancellationToken);
    }
}
