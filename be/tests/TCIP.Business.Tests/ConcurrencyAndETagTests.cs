using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Tests.TestDoubles;
using TCIP.Common.Exceptions;
using Xunit;

namespace TCIP.Business.Tests;

public sealed class ConcurrencyAndETagTests
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
    public async Task UpdateEvent_NoOpUpdate_ReturnsCurrentVersionWithoutIncrement()
    {
        var (service, _, user) = CreateTestContext();

        var start = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var created = await service.CreateEventAsync(new CreateEventRequest
        {
            StartAt = start,
            Translations = [new EventTranslationRequest("en", "Meeting", "Description")]
        }, user.Id, default);

        Assert.Equal(1L, created.Version);

        // Identical update (no state change)
        var updated = await service.UpdateEventAsync(created.Id, new UpdateEventRequest
        {
            StartAt = start,
            Translations = [new EventTranslationRequest("en", "Meeting", "Description")]
        }, 1L, user.Id, default);

        Assert.Equal(1L, updated.Version);
    }

    [Fact]
    public async Task UpdateEvent_EndAtEarlierOrEqualStartAt_ThrowsBadRequestException()
    {
        var (service, _, user) = CreateTestContext();

        var start = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateEventAsync(new CreateEventRequest
            {
                StartAt = start,
                EndAt = start,
                Translations = [new EventTranslationRequest("en", "Meeting", null)]
            }, user.Id, default));
    }

    [Fact]
    public async Task CreateEvent_DuplicateTranslations_ThrowsBadRequestException()
    {
        var (service, _, user) = CreateTestContext();

        var start = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateEventAsync(new CreateEventRequest
            {
                StartAt = start,
                Translations = [
                    new EventTranslationRequest("en", "English 1", null),
                    new EventTranslationRequest("EN", "English 2", null)
                ]
            }, user.Id, default));
    }

    [Fact]
    public async Task UpdateEvent_SeriesChange_CleansOrphanExceptions()
    {
        var (service, repo, user) = CreateTestContext();

        var start = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero); // Monday
        var created = await service.CreateEventAsync(new CreateEventRequest
        {
            StartAt = start,
            RecurrenceRule = "RRULE:FREQ=WEEKLY;BYDAY=MO",
            Translations = [new EventTranslationRequest("en", "Weekly Monday", null)]
        }, user.Id, default);

        var ev = repo.Events[created.Id];
        var mondaySept14 = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
        ev.OccurrenceExceptions.Add(new EventOccurrenceException
        {
            EventId = ev.Id,
            OriginalStartAtUtc = mondaySept14,
            IsCancelled = true
        });

        // Change series to a different recurrence rule
        var fridaySept11 = new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);
        var updated = await service.UpdateEventAsync(created.Id, new UpdateEventRequest
        {
            StartAt = fridaySept11,
            RecurrenceRule = "RRULE:FREQ=DAILY",
            Translations = [new EventTranslationRequest("en", "Daily", null)]
        }, 1L, user.Id, default);

        Assert.Equal(2L, updated.Version);
    }
}
