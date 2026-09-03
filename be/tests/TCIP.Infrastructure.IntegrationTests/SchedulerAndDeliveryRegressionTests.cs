using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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

public sealed class SchedulerAndDeliveryRegressionTests
{
    private static TcipDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TcipDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TcipDbContext(options);
    }

    [Fact]
    public void Planner_ReprojectSchedule_UpdatesOccurrenceAndNextFire()
    {
        var recurrence = new RecurrenceEngine();
        var planner = new ReminderSchedulePlanner(recurrence);

        var startUtc = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = startUtc,
            RecurrenceRule = "RRULE:FREQ=WEEKLY;BYDAY=MO",
            TimeZoneId = "UTC",
            Status = EventStatus.Active,
            Version = 1
        };

        var rule = new ReminderRule
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RemindBeforeMinutes = 30,
            Status = ReminderRuleStatus.Active,
            MaxLatenessMinutes = 15
        };

        var now = new DateTimeOffset(2026, 9, 7, 8, 0, 0, TimeSpan.Zero);
        planner.InitializeOrRebuildSchedule(rule, ev, now);

        Assert.NotNull(rule.Schedule);
        Assert.Equal(startUtc, rule.Schedule.OccurrenceStartAtUtc);
        Assert.Equal(startUtc.AddMinutes(-30), rule.Schedule.NextFireAtUtc);

        // Add exception moving occurrence to 14:00
        var movedStart = new DateTimeOffset(2026, 9, 7, 14, 0, 0, TimeSpan.Zero);
        ev.OccurrenceExceptions.Add(new EventOccurrenceException
        {
            EventId = ev.Id,
            OriginalStartAtUtc = startUtc,
            IsCancelled = false,
            StartAtUtc = movedStart
        });
        ev.Version = 2;

        planner.ReprojectSchedule(rule, ev, now);

        Assert.Equal(startUtc, rule.Schedule.OccurrenceStartAtUtc);
        Assert.Equal(movedStart.AddMinutes(-30), rule.Schedule.NextFireAtUtc);
    }

    [Fact]
    public void DispatchValidator_SubDayDispatches_FailsValidation()
    {
        var recurrence = new RecurrenceEngine();
        var validator = new ReminderDispatchValidator(recurrence);

        var start = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = start,
            TimeZoneId = "UTC",
            Status = EventStatus.Active,
            Version = 1L
        };
        var rule = new ReminderRule
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RemindBeforeMinutes = 15,
            RepeatEveryMinutes = 5,
            Status = ReminderRuleStatus.Active
        };

        // 09:46 is not aligned to the five-minute cadence from 09:45.
        var result = validator.ValidateDispatch(
            ev,
            rule,
            ev.Id,
            rule.Id,
            start,
            start,
            start.AddMinutes(-14));

        Assert.False(result.IsValid);
        Assert.Contains("repeat cadence", result.DropReason);
    }

    [Fact]
    public async Task NotificationDelivery_NullOrEmptyTranslations_UsesFallbackTitle()
    {
        var db = CreateInMemoryDbContext();
        var user = new User { Id = Guid.NewGuid(), PrincipalId = Guid.NewGuid(), Email = "test@test.com", DisplayName = "User", PasswordHash = "hash", Language = "fr" };
        db.Users.Add(user);

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            CreatedById = user.Id,
            StartAtUtc = DateTimeOffset.UtcNow,
            TimeZoneId = "UTC",
            Status = EventStatus.Active
        };
        var rule = new ReminderRule
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RemindBeforeMinutes = 15,
            Status = ReminderRuleStatus.Active
        };
        db.Events.Add(ev);
        db.ReminderRules.Add(rule);
        await db.SaveChangesAsync();

        var recurrence = new RecurrenceEngine();
        var validator = new ReminderDispatchValidator(recurrence);
        var config = new ConfigurationBuilder().Build();
        var fakeGateway = new TestGateway();
        var service = new NotificationDeliveryService(db, fakeGateway, validator, config, TimeProvider.System, NullLogger<NotificationDeliveryService>.Instance);

        var batch = new NotificationBatchMessage(
            Guid.NewGuid(),
            rule.Id,
            ev.Id,
            1L,
            ev.StartAtUtc,
            ev.StartAtUtc,
            ev.StartAtUtc.AddMinutes(-15),
            [user.Id]);

        await service.DeliverBatchAsync(batch, default);

        Assert.Single(fakeGateway.Notifications);
        Assert.Equal("Event Reminder", fakeGateway.Notifications[0].Title);
    }

    private sealed class TestGateway : INotificationGateway
    {
        public readonly List<NotificationResponse> Notifications = new();

        public Task SendNotificationToUserAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }
    }
}
