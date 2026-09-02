using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Infrastructure.Adapters.Recurrence;
using TCIP.Infrastructure.Data;
using TCIP.Infrastructure.Services;
using TCIP.Infrastructure.Workers;
using Xunit;

namespace TCIP.Infrastructure.IntegrationTests;

public sealed class PostgresIntegrationTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=identity;Username=identity;Password=identity";

    private static TcipDbContext CreatePostgresDbContext()
    {
        var options = new DbContextOptionsBuilder<TcipDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        var db = new TcipDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static bool IsPostgresAvailable()
    {
        try
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public async Task Postgres_SkipLocked_DualConcurrentSchedulers_DoNotDuplicateOutbox()
    {
        if (!IsPostgresAvailable()) return;

        using var db = CreatePostgresDbContext();
        var userPrincipal = new Principal { Id = Guid.NewGuid(), Type = PrincipalType.User, Available = true };
        var user = new User { Id = Guid.NewGuid(), PrincipalId = userPrincipal.Id, Email = $"user_{Guid.NewGuid():N}@test.com", DisplayName = "Test User", PasswordHash = "hash" };
        db.Principals.Add(userPrincipal);
        db.Users.Add(user);

        var nowUtc = DateTimeOffset.UtcNow;
        var startAt = new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, nowUtc.Minute, 0, TimeSpan.Zero).AddMinutes(10);
        var dueFire = startAt.AddMinutes(-12);

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            CreatedById = user.Id,
            StartAtUtc = startAt,
            TimeZoneId = "UTC",
            Status = EventStatus.Active,
            Version = 1L,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
        var rule = new ReminderRule
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RemindBeforeMinutes = 12,
            Status = ReminderRuleStatus.Active,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
        var schedule = new ReminderSchedule
        {
            ReminderRuleId = rule.Id,
            OccurrenceStartAtUtc = startAt,
            NextFireAtUtc = dueFire,
            RepeatIndex = 0,
            EventVersion = 1L,
            Status = ReminderScheduleStatus.Active,
            UpdatedAtUtc = nowUtc
        };

        db.Events.Add(ev);
        db.ReminderRules.Add(rule);
        db.ReminderSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddDbContext<TcipDbContext>(opt => opt.UseNpgsql(ConnectionString));
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Kafka:ReminderDueTopic"] = "calendar.reminder-due.v2"
        }).Build();

        var recurrence = new RecurrenceEngine();
        var planner = new ReminderSchedulePlanner(recurrence);
        var scheduler1 = new ReminderSchedulerWorker(scopeFactory, config, recurrence, planner, TimeProvider.System, NullLogger<ReminderSchedulerWorker>.Instance);
        var scheduler2 = new ReminderSchedulerWorker(scopeFactory, config, recurrence, planner, TimeProvider.System, NullLogger<ReminderSchedulerWorker>.Instance);

        var task1 = scheduler1.ScheduleBatchAsync(default);
        var task2 = scheduler2.ScheduleBatchAsync(default);
        await Task.WhenAll(task1, task2);

        using var verifyDb = CreatePostgresDbContext();
        var outboxMessages = await verifyDb.OutboxMessages
            .Where(x => x.ReminderRuleId == rule.Id)
            .ToListAsync();

        Assert.Single(outboxMessages);
    }

    [Fact]
    public async Task Postgres_OnConflictDoNothing_AbsorbsDuplicateBatchDeliveries()
    {
        if (!IsPostgresAvailable()) return;

        using var db = CreatePostgresDbContext();
        var userPrincipal = new Principal { Id = Guid.NewGuid(), Type = PrincipalType.User, Available = true };
        var user = new User { Id = Guid.NewGuid(), PrincipalId = userPrincipal.Id, Email = $"user_{Guid.NewGuid():N}@test.com", DisplayName = "Test User", PasswordHash = "hash" };
        db.Principals.Add(userPrincipal);
        db.Users.Add(user);

        var startAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            CreatedById = user.Id,
            StartAtUtc = startAt,
            TimeZoneId = "UTC",
            Status = EventStatus.Active,
            Version = 1L,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var rule = new ReminderRule
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RemindBeforeMinutes = 15,
            Status = ReminderRuleStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Events.Add(ev);
        db.ReminderRules.Add(rule);
        await db.SaveChangesAsync();

        var batch = new NotificationBatchMessage(
            Guid.NewGuid(),
            rule.Id,
            ev.Id,
            1L,
            startAt,
            startAt,
            startAt.AddMinutes(-15),
            0,
            [user.Id]);

        var recurrence = new RecurrenceEngine();
        var validator = new ReminderDispatchValidator(recurrence);
        var config = new ConfigurationBuilder().Build();
        var deliveryService = new NotificationDeliveryService(db, new FakeNotificationGateway(), validator, config, TimeProvider.System, NullLogger<NotificationDeliveryService>.Instance);

        await deliveryService.DeliverBatchAsync(batch, default);

        var count1 = await db.Notifications.CountAsync(x => x.ReminderRuleId == rule.Id && x.RecipientUserId == user.Id);
        Assert.Equal(1, count1);

        await deliveryService.DeliverBatchAsync(batch, default);

        var count2 = await db.Notifications.CountAsync(x => x.ReminderRuleId == rule.Id && x.RecipientUserId == user.Id);
        Assert.Equal(1, count2);
    }

    private sealed class FakeNotificationGateway : INotificationGateway
    {
        public Task SendNotificationToUserAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
