using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases.Events;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Tests.TestDoubles;
using TCIP.Common.Exceptions;
using Xunit;

namespace TCIP.Business.Tests;

public sealed class VersioningAndETagTests
{
    private sealed record TestContext(
        CreateEventUseCase CreateEvent,
        UpdateEventUseCase UpdateEvent,
        InMemoryCalendarRepository Repo,
        User User);

    private static TestContext CreateTestContext()
    {
        var repo = new InMemoryCalendarRepository();
        var recurrence = new SimpleTestRecurrenceEngine();
        var planner = new ReminderSchedulePlanner(recurrence);
        var createService = new CreateEventUseCase(repo, recurrence, planner, repo, TimeProvider.System);
        var updateService = new UpdateEventUseCase(repo, recurrence, planner, TimeProvider.System);

        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", DisplayName = "Test", PasswordHash = "hash" };
        repo.Users[user.Id] = user;

        return new TestContext(createService, updateService, repo, user);
    }

    [Fact]
    public async Task UpdateEvent_StaleIfMatch_ThrowsPreconditionFailedException()
    {
        var ctx = CreateTestContext();

        var created = await ctx.CreateEvent.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, ctx.User.Id, default);

        var updateReq = new UpdateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 11, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Updated", null)]
        };

        // Current version is 1, pass stale expected version 99
        await Assert.ThrowsAsync<PreconditionFailedException>(() =>
            ctx.UpdateEvent.UpdateEventAsync(created.Id, updateReq, 99L, ctx.User.Id, default));
    }

    [Fact]
    public async Task UpdateEvent_MatchingIfMatch_IncrementsVersionAtomically()
    {
        var ctx = CreateTestContext();

        var created = await ctx.CreateEvent.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, ctx.User.Id, default);

        var updateReq = new UpdateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 11, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Updated", null)]
        };

        var updated = await ctx.UpdateEvent.UpdateEventAsync(created.Id, updateReq, 1L, ctx.User.Id, default);

        Assert.Equal(2L, updated.Version);
    }

    [Fact]
    public async Task UpdateEvent_SchedulingChange_SyncsReminderSchedule()
    {
        var ctx = CreateTestContext();

        var start1 = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var created = await ctx.CreateEvent.CreateEventAsync(new CreateEventRequest
        {
            StartAt = start1,
            Translations = [new EventTranslationRequest("en", "Meeting", null)],
            ReminderRules = [new CreateReminderRuleRequest(15)]
        }, ctx.User.Id, default);

        var eventWithSchedule = ctx.Repo.Events[created.Id];
        var schedule = eventWithSchedule.ReminderRules.Single().Schedule!;
        Assert.Equal(start1.AddMinutes(-15), schedule.NextFireAtUtc);

        // Move event to 14:00
        var start2 = new DateTimeOffset(2026, 9, 2, 14, 0, 0, TimeSpan.Zero);
        var updated = await ctx.UpdateEvent.UpdateEventAsync(created.Id, new UpdateEventRequest
        {
            StartAt = start2,
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, 1L, ctx.User.Id, default);

        var updatedSchedule = ctx.Repo.Events[created.Id].ReminderRules.Single().Schedule!;
        Assert.Equal(start2.AddMinutes(-15), updatedSchedule.NextFireAtUtc);
        Assert.Equal(2L, updatedSchedule.EventVersion);
    }
}
