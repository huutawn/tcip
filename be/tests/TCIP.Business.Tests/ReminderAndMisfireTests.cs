using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases.Events;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Tests.TestDoubles;
using Xunit;

namespace TCIP.Business.Tests;

public sealed class ReminderAndMisfireTests
{
    private static (CreateEventUseCase CommandService, InMemoryCalendarRepository Repo, User User) CreateTestContext()
    {
        var repo = new InMemoryCalendarRepository();
        var recurrence = new SimpleTestRecurrenceEngine();
        var planner = new ReminderSchedulePlanner(recurrence);
        var service = new CreateEventUseCase(repo, recurrence, planner, repo, TimeProvider.System);

        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", DisplayName = "Test", PasswordHash = "hash" };
        repo.Users[user.Id] = user;

        return (service, repo, user);
    }

    [Fact]
    public async Task CreateReminderRule_RepeatInterval_CreatesAnOpenEndedRepeatRule()
    {
        var (service, repo, user) = CreateTestContext();

        var repeatRuleReq = new CreateReminderRuleRequest(
            RemindBeforeMinutes: 30,
            RepeatEveryMinutes: 15);

        var created = await service.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)],
            ReminderRules = [repeatRuleReq]
        }, user.Id, default);

        Assert.Equal(15, repo.Events[created.Id].ReminderRules.Single().RepeatEveryMinutes);
    }

    [Fact]
    public async Task CreateReminderRule_ValidRepeatRule_CreatesInitialSchedule()
    {
        var (service, repo, user) = CreateTestContext();

        var start = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        // The scheduler continues every 20 minutes until the occurrence starts.
        var validRuleReq = new CreateReminderRuleRequest(
            RemindBeforeMinutes: 60,
            RepeatEveryMinutes: 20);

        var created = await service.CreateEventAsync(new CreateEventRequest
        {
            StartAt = start,
            Translations = [new EventTranslationRequest("en", "Meeting", null)],
            ReminderRules = [validRuleReq]
        }, user.Id, default);

        var ev = repo.Events[created.Id];
        var schedule = ev.ReminderRules.Single().Schedule!;

        Assert.Equal(start.AddMinutes(-60), schedule.NextFireAtUtc);
        Assert.Equal(ReminderScheduleStatus.Active, schedule.Status);
    }

    [Fact]
    public void ReminderSchedulePlanner_Rebuild_DoesNotJumpToDistantPast()
    {
        var recurrence = new SimpleTestRecurrenceEngine();
        var planner = new ReminderSchedulePlanner(recurrence);

        // Event started 1 year ago, recurring daily
        var startUtc = new DateTimeOffset(2025, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            StartAtUtc = startUtc,
            RecurrenceRule = "RRULE:FREQ=DAILY",
            TimeZoneId = "UTC",
            Status = EventStatus.Active,
            Version = 2
        };

        var rule = new ReminderRule
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RemindBeforeMinutes = 15,
            Status = ReminderRuleStatus.Active,
            MaxLatenessMinutes = 15
        };

        var now = new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

        planner.InitializeOrRebuildSchedule(rule, ev, now, isRebuild: true);

        Assert.NotNull(rule.Schedule);
        Assert.True(rule.Schedule.OccurrenceStartAtUtc >= now - TimeSpan.FromHours(2));
        Assert.Equal(new DateTimeOffset(2026, 9, 3, 10, 0, 0, TimeSpan.Zero), rule.Schedule.OccurrenceStartAtUtc);
    }
}
