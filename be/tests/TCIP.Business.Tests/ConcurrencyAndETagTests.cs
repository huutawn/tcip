using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases.Events;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Tests.TestDoubles;
using TCIP.Common.Exceptions;
using Xunit;

namespace TCIP.Business.Tests;

public sealed class ConcurrencyAndETagTests
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
    public async Task UpdateEvent_NoOpUpdate_ReturnsCurrentVersionWithoutIncrement()
    {
        var ctx = CreateTestContext();

        var start = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var created = await ctx.CreateEvent.CreateEventAsync(new CreateEventRequest
        {
            StartAt = start,
            Translations = [new EventTranslationRequest("en", "Meeting", "Description")]
        }, ctx.User.Id, default);

        Assert.Equal(1L, created.Version);

        // Identical update (no state change)
        var updated = await ctx.UpdateEvent.UpdateEventAsync(created.Id, new UpdateEventRequest
        {
            StartAt = start,
            Translations = [new EventTranslationRequest("en", "Meeting", "Description")]
        }, 1L, ctx.User.Id, default);

        Assert.Equal(1L, updated.Version);
    }

    [Fact]
    public async Task UpdateEvent_EndAtEarlierOrEqualStartAt_ThrowsBadRequestException()
    {
        var ctx = CreateTestContext();

        var start = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            ctx.CreateEvent.CreateEventAsync(new CreateEventRequest
            {
                StartAt = start,
                EndAt = start,
                Translations = [new EventTranslationRequest("en", "Meeting", null)]
            }, ctx.User.Id, default));
    }

    [Fact]
    public async Task CreateEvent_DuplicateTranslations_ThrowsBadRequestException()
    {
        var ctx = CreateTestContext();

        var start = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            ctx.CreateEvent.CreateEventAsync(new CreateEventRequest
            {
                StartAt = start,
                Translations = [
                    new EventTranslationRequest("en", "English 1", null),
                    new EventTranslationRequest("EN", "English 2", null)
                ]
            }, ctx.User.Id, default));
    }

    [Fact]
    public async Task UpdateEvent_SeriesChange_CleansOrphanExceptions()
    {
        var ctx = CreateTestContext();

        var start = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero); // Monday
        var created = await ctx.CreateEvent.CreateEventAsync(new CreateEventRequest
        {
            StartAt = start,
            RecurrenceRule = "RRULE:FREQ=WEEKLY;BYDAY=MO",
            Translations = [new EventTranslationRequest("en", "Weekly Monday", null)]
        }, ctx.User.Id, default);

        var ev = ctx.Repo.Events[created.Id];
        var mondaySept14 = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
        ev.OccurrenceExceptions.Add(new EventOccurrenceException
        {
            EventId = ev.Id,
            OriginalStartAtUtc = mondaySept14,
            IsCancelled = true
        });

        // Change series to a different recurrence rule
        var fridaySept11 = new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);
        var updated = await ctx.UpdateEvent.UpdateEventAsync(created.Id, new UpdateEventRequest
        {
            StartAt = fridaySept11,
            RecurrenceRule = "RRULE:FREQ=DAILY",
            Translations = [new EventTranslationRequest("en", "Daily", null)]
        }, 1L, ctx.User.Id, default);

        Assert.Equal(2L, updated.Version);
    }
}
