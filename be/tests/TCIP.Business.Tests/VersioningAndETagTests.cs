using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Tests.TestDoubles;
using TCIP.Common.Exceptions;
using Xunit;

namespace TCIP.Business.Tests;

public sealed class VersioningAndETagTests
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
    public async Task UpdateEvent_StaleIfMatch_ThrowsPreconditionFailedException()
    {
        var (service, _, user) = CreateTestContext();

        var created = await service.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, user.Id, default);

        var updateReq = new UpdateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 11, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Updated Meeting", null)]
        };

        // Current version is 1, pass stale expected version 99
        await Assert.ThrowsAsync<PreconditionFailedException>(() =>
            service.UpdateEventAsync(created.Id, updateReq, 99L, user.Id, default));
    }

    [Fact]
    public async Task UpdateEvent_MatchingIfMatch_IncrementsVersionAtomically()
    {
        var (service, _, user) = CreateTestContext();

        var created = await service.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, user.Id, default);

        Assert.Equal(1L, created.Version);

        var updateReq = new UpdateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Updated Title", null)]
        };

        var updated = await service.UpdateEventAsync(created.Id, updateReq, 1L, user.Id, default);
        Assert.Equal(2L, updated.Version);
    }

    [Fact]
    public async Task UpdateEvent_SchedulingChange_SyncsReminderSchedule()
    {
        var (service, repo, user) = CreateTestContext();

        var start1 = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var created = await service.CreateEventAsync(new CreateEventRequest
        {
            StartAt = start1,
            Translations = [new EventTranslationRequest("en", "Meeting", null)],
            ReminderRules = [new CreateReminderRuleRequest(15)]
        }, user.Id, default);

        var eventWithSchedule = repo.Events[created.Id];
        var schedule = eventWithSchedule.ReminderRules.Single().Schedule!;
        Assert.Equal(start1.AddMinutes(-15), schedule.NextFireAtUtc);

        // Move event to 14:00
        var start2 = new DateTimeOffset(2026, 9, 2, 14, 0, 0, TimeSpan.Zero);
        var updated = await service.UpdateEventAsync(created.Id, new UpdateEventRequest
        {
            StartAt = start2,
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, 1L, user.Id, default);

        var updatedSchedule = repo.Events[created.Id].ReminderRules.Single().Schedule!;
        Assert.Equal(start2.AddMinutes(-15), updatedSchedule.NextFireAtUtc);
        Assert.Equal(2L, updatedSchedule.EventVersion);
    }
}
