using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Tests.TestDoubles;
using TCIP.Common.Exceptions;
using Xunit;

namespace TCIP.Business.Tests;

public sealed class ReminderAndMisfireTests
{
    private static (EventCommandUseCase CommandService, InMemoryCalendarRepository Repo, User User) CreateTestContext()
    {
        var repo = new InMemoryCalendarRepository();
        var recurrence = new SimpleTestRecurrenceEngine();
        var planner = new ReminderSchedulePlanner(recurrence);
        var service = new EventCommandUseCase(repo, recurrence, planner, TimeProvider.System);

        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", DisplayName = "Test", PasswordHash = "hash" };
        repo.Users[user.Id] = user;

        return (service, repo, user);
    }

    [Fact]
    public async Task CreateReminderRule_LastRepeatExceedsOccurrenceStart_ThrowsBadRequestException()
    {
        var (service, _, user) = CreateTestContext();

        // RemindBefore = 30 min, RepeatCount = 3, RepeatEvery = 15 min => 3 * 15 = 45 min >= 30 min! (Invalid)
        var invalidRuleReq = new CreateReminderRuleRequest(
            RemindBeforeMinutes: 30,
            RepeatEveryMinutes: 15,
            RepeatCount: 3);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateEventAsync(new CreateEventRequest
            {
                StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
                Translations = [new EventTranslationRequest("en", "Meeting", null)],
                ReminderRules = [invalidRuleReq]
            }, user.Id, default));
    }

    [Fact]
    public async Task CreateReminderRule_ValidRepeatRule_CreatesScheduleWithInitialRepeatIndexZero()
    {
        var (service, repo, user) = CreateTestContext();

        var start = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        // RemindBefore = 60 min, RepeatCount = 2, RepeatEvery = 20 min (fires at -60m, -40m, -20m < start)
        var validRuleReq = new CreateReminderRuleRequest(
            RemindBeforeMinutes: 60,
            RepeatEveryMinutes: 20,
            RepeatCount: 2);

        var created = await service.CreateEventAsync(new CreateEventRequest
        {
            StartAt = start,
            Translations = [new EventTranslationRequest("en", "Meeting", null)],
            ReminderRules = [validRuleReq]
        }, user.Id, default);

        var ev = repo.Events[created.Id];
        var schedule = ev.ReminderRules.Single().Schedule!;

        Assert.Equal(0, schedule.RepeatIndex);
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
