using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Services;

public interface INotificationDeliveryService
{
    Task DeliverBatchAsync(NotificationBatchMessage message, CancellationToken cancellationToken);
}

public sealed class NotificationDeliveryService(
    TcipDbContext dbContext,
    INotificationGateway notificationGateway,
    IReminderDispatchValidator dispatchValidator,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<NotificationDeliveryService> logger) : INotificationDeliveryService
{
    private readonly int signalRConcurrency = Math.Max(
        1,
        configuration.GetValue<int?>("Notifications:SignalRConcurrency") ?? 64);

    public async Task DeliverBatchAsync(NotificationBatchMessage message, CancellationToken cancellationToken)
    {
        if (message.RecipientUserIds.Count == 0 || message.RecipientUserIds.Count > 1000)
        {
            logger.LogWarning("Rejecting invalid batch delivery with recipient count {Count}.", message.RecipientUserIds.Count);
            return;
        }

        var calendarEvent = await dbContext.Events
            .AsNoTracking()
            .Include(x => x.OccurrenceExceptions)
            .SingleOrDefaultAsync(x => x.Id == message.EventId, cancellationToken);

        var rule = await dbContext.ReminderRules
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == message.ReminderRuleId, cancellationToken);

        var validation = dispatchValidator.ValidateDispatch(
            calendarEvent,
            rule,
            message.EventId,
            message.ReminderRuleId,
            message.OriginalStartAtUtc,
            message.EffectiveStartAtUtc,
            message.ScheduledFireAtUtc,
            message.RepeatIndex);

        if (!validation.IsValid)
        {
            logger.LogInformation("Dropping batch delivery for event {EventId}, rule {RuleId}: {Reason}", message.EventId, message.ReminderRuleId, validation.DropReason);
            return;
        }

        var now = timeProvider.GetUtcNow();

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(u => message.RecipientUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Language, cancellationToken);

        if (users.Count == 0)
        {
            return;
        }

        var translations = await dbContext.EventTranslations
            .AsNoTracking()
            .Where(t => t.EventId == message.EventId)
            .ToListAsync(cancellationToken);

        var rowsToInsert = new List<(Guid Id, Guid RecipientUserId, string Title, string? Description)>();
        foreach (var recipientId in message.RecipientUserIds.Distinct())
        {
            if (!users.TryGetValue(recipientId, out var userLang))
            {
                continue;
            }

            var (title, description) = SelectTranslation(translations, userLang);
            rowsToInsert.Add((Guid.NewGuid(), recipientId, title, description));
        }

        if (rowsToInsert.Count == 0)
        {
            return;
        }

        IReadOnlyList<InsertedNotification> insertedRows;
        if (!dbContext.Database.IsRelational())
        {
            var inMemoryInserted = new List<InsertedNotification>();
            foreach (var item in rowsToInsert)
            {
                var existing = await dbContext.Notifications.AnyAsync(n =>
                    n.ReminderRuleId == message.ReminderRuleId &&
                    n.OriginalStartAtUtc == message.OriginalStartAtUtc &&
                    n.RepeatIndex == message.RepeatIndex &&
                    n.RecipientUserId == item.RecipientUserId, cancellationToken);

                if (!existing)
                {
                    dbContext.Notifications.Add(new Notification
                    {
                        Id = item.Id,
                        ReminderRuleId = message.ReminderRuleId,
                        EventId = message.EventId,
                        RecipientUserId = item.RecipientUserId,
                        OriginalStartAtUtc = message.OriginalStartAtUtc,
                        EffectiveStartAtUtc = message.EffectiveStartAtUtc,
                        ScheduledFireAtUtc = message.ScheduledFireAtUtc,
                        RepeatIndex = message.RepeatIndex,
                        Title = item.Title,
                        Description = item.Description,
                        SentAtUtc = now
                    });
                    inMemoryInserted.Add(new InsertedNotification(item.Id, item.RecipientUserId, item.Title, item.Description, now));
                }
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            insertedRows = inMemoryInserted;
        }
        else
        {
            insertedRows = await InsertNotificationsBulkAsync(rowsToInsert, message, now, cancellationToken);
        }

        if (insertedRows.Count == 0)
        {
            return;
        }

        var semaphore = new SemaphoreSlim(signalRConcurrency);
        var tasks = insertedRows.Select(async row =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await notificationGateway.SendNotificationToUserAsync(
                    row.RecipientUserId,
                    new NotificationResponse(
                        row.Id,
                        message.EventId,
                        message.ReminderRuleId,
                        message.OriginalStartAtUtc,
                        message.EffectiveStartAtUtc,
                        message.ScheduledFireAtUtc,
                        message.RepeatIndex,
                        row.Title,
                        row.Description,
                        row.SentAtUtc,
                        null),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send SignalR notification to user {UserId}.", row.RecipientUserId);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task<IReadOnlyList<InsertedNotification>> InsertNotificationsBulkAsync(
        IReadOnlyList<(Guid Id, Guid RecipientUserId, string Title, string? Description)> rows,
        NotificationBatchMessage message,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var cmd = connection.CreateCommand();
        var valuesClauses = new List<string>(rows.Count);
        var index = 0;

        foreach (var item in rows)
        {
            var pId = $"@p{index++}";
            var pRuleId = $"@p{index++}";
            var pEventId = $"@p{index++}";
            var pUserId = $"@p{index++}";
            var pOrigStart = $"@p{index++}";
            var pEffStart = $"@p{index++}";
            var pSchedFire = $"@p{index++}";
            var pRepeatIndex = $"@p{index++}";
            var pTitle = $"@p{index++}";
            var pDesc = $"@p{index++}";
            var pSentAt = $"@p{index++}";

            valuesClauses.Add($"({pId}, {pRuleId}, {pEventId}, {pUserId}, {pOrigStart}, {pEffStart}, {pSchedFire}, {pRepeatIndex}, {pTitle}, {pDesc}, {pSentAt})");

            cmd.Parameters.Add(new NpgsqlParameter(pId, NpgsqlDbType.Uuid) { Value = item.Id });
            cmd.Parameters.Add(new NpgsqlParameter(pRuleId, NpgsqlDbType.Uuid) { Value = message.ReminderRuleId });
            cmd.Parameters.Add(new NpgsqlParameter(pEventId, NpgsqlDbType.Uuid) { Value = message.EventId });
            cmd.Parameters.Add(new NpgsqlParameter(pUserId, NpgsqlDbType.Uuid) { Value = item.RecipientUserId });
            cmd.Parameters.Add(new NpgsqlParameter(pOrigStart, NpgsqlDbType.TimestampTz) { Value = message.OriginalStartAtUtc });
            cmd.Parameters.Add(new NpgsqlParameter(pEffStart, NpgsqlDbType.TimestampTz) { Value = message.EffectiveStartAtUtc });
            cmd.Parameters.Add(new NpgsqlParameter(pSchedFire, NpgsqlDbType.TimestampTz) { Value = message.ScheduledFireAtUtc });
            cmd.Parameters.Add(new NpgsqlParameter(pRepeatIndex, NpgsqlDbType.Integer) { Value = message.RepeatIndex });
            cmd.Parameters.Add(new NpgsqlParameter(pTitle, NpgsqlDbType.Varchar) { Value = item.Title });
            cmd.Parameters.Add(new NpgsqlParameter(pDesc, NpgsqlDbType.Text) { Value = (object?)item.Description ?? DBNull.Value });
            cmd.Parameters.Add(new NpgsqlParameter(pSentAt, NpgsqlDbType.TimestampTz) { Value = now });
        }

        cmd.CommandText = $"""
            INSERT INTO notifications (
                id, reminder_rule_id, event_id, recipient_user_id,
                original_start_at_utc, effective_start_at_utc, scheduled_fire_at_utc,
                repeat_index, title, description, sent_at_utc
            )
            VALUES {string.Join(",\n", valuesClauses)}
            ON CONFLICT (reminder_rule_id, original_start_at_utc, repeat_index, recipient_user_id) DO NOTHING
            RETURNING id, recipient_user_id, title, description, sent_at_utc;
            """;

        var results = new List<InsertedNotification>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new InsertedNotification(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetFieldValue<DateTimeOffset>(4)));
        }

        return results;
    }

    private static (string Title, string? Description) SelectTranslation(
        IReadOnlyList<EventTranslation> translations,
        string userLanguage)
    {
        if (translations.Count == 0)
            return ("Event Reminder", null);

        var exact = translations.FirstOrDefault(t => string.Equals(t.Language, userLanguage, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return (exact.Title, exact.Description);

        var baseUserLang = userLanguage.Split('-')[0];
        var baseMatch = translations.FirstOrDefault(t => string.Equals(t.Language.Split('-')[0], baseUserLang, StringComparison.OrdinalIgnoreCase));
        if (baseMatch is not null)
            return (baseMatch.Title, baseMatch.Description);

        var english = translations.FirstOrDefault(t => string.Equals(t.Language, "en", StringComparison.OrdinalIgnoreCase));
        if (english is not null)
            return (english.Title, english.Description);

        var first = translations.First();
        return (first.Title, first.Description);
    }

    private sealed record InsertedNotification(
        Guid Id,
        Guid RecipientUserId,
        string Title,
        string? Description,
        DateTimeOffset SentAtUtc);
}
