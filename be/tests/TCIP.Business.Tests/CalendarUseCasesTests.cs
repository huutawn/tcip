using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases.Events;
using TCIP.Business.Modules.Calendar.Application.UseCases.Notifications;
using TCIP.Business.Modules.Calendar.Application.UseCases.Occurrences;
using TCIP.Business.Modules.Calendar.Application.UseCases.Reminders;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Tests.TestDoubles;
using TCIP.Common.Exceptions;
using Xunit;

namespace TCIP.Business.Tests;

public sealed class CalendarUseCasesTests
{
    [Fact]
    public async Task CreateEvent_AudienceUnavailable_ThrowsBadRequest()
    {
        var repo = new InMemoryCalendarRepository();
        var recurrence = new SimpleTestRecurrenceEngine();
        var planner = new ReminderSchedulePlanner(recurrence);
        var useCase = new CreateEventUseCase(repo, recurrence, planner, repo, TimeProvider.System);

        var userId = Guid.NewGuid();
        var unavailablePrincipalId = Guid.NewGuid();
        // Do not add to repo.Principals as available

        await Assert.ThrowsAsync<BadRequestException>(() =>
            useCase.CreateEventAsync(new CreateEventRequest
            {
                StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
                Translations = [new EventTranslationRequest("en", "Event", null)],
                AudiencePrincipalIds = [unavailablePrincipalId]
            }, userId, default));
    }

    [Fact]
    public async Task CancelEvent_IdempotentOnSubsequentCalls()
    {
        var repo = new InMemoryCalendarRepository();
        var recurrence = new SimpleTestRecurrenceEngine();
        var planner = new ReminderSchedulePlanner(recurrence);
        var createUseCase = new CreateEventUseCase(repo, recurrence, planner, repo, TimeProvider.System);
        var cancelUseCase = new CancelEventUseCase(repo, TimeProvider.System);

        var userId = Guid.NewGuid();
        var created = await createUseCase.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Event", null)]
        }, userId, default);

        var v1 = await cancelUseCase.CancelEventAsync(created.Id, 1L, userId, default);
        Assert.Equal(2L, v1);

        // Calling cancel on already cancelled event returns current version without increment
        var v2 = await cancelUseCase.CancelEventAsync(created.Id, 2L, userId, default);
        Assert.Equal(2L, v2);
    }

    [Fact]
    public async Task ReminderRule_Validation_NegativeValuesThrow()
    {
        var repo = new InMemoryCalendarRepository();
        var recurrence = new SimpleTestRecurrenceEngine();
        var planner = new ReminderSchedulePlanner(recurrence);
        var createUseCase = new CreateEventUseCase(repo, recurrence, planner, repo, TimeProvider.System);
        var addRuleUseCase = new AddReminderRuleUseCase(repo, planner, TimeProvider.System);

        var userId = Guid.NewGuid();
        var created = await createUseCase.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Event", null)]
        }, userId, default);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            addRuleUseCase.AddReminderRuleAsync(created.Id, new CreateReminderRuleRequest(-5), 1L, userId, default));
    }

    [Fact]
    public async Task OccurrenceException_SeriesMismatch_ThrowsBadRequest()
    {
        var repo = new InMemoryCalendarRepository();
        var recurrence = new SimpleTestRecurrenceEngine();
        var planner = new ReminderSchedulePlanner(recurrence);
        var createUseCase = new CreateEventUseCase(repo, recurrence, planner, repo, TimeProvider.System);
        var occUseCase = new UpsertOccurrenceExceptionUseCase(repo, recurrence, planner, TimeProvider.System);

        var userId = Guid.NewGuid();
        var created = await createUseCase.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Event", null)]
        }, userId, default);

        var alienDate = new DateTimeOffset(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            occUseCase.UpsertOccurrenceExceptionAsync(created.Id, new UpsertOccurrenceExceptionRequest(alienDate, true), 1L, userId, default));
    }

    [Fact]
    public async Task Notifications_GetAndMarkRead_Idempotent()
    {
        var repo = new InMemoryCalendarRepository();
        var timeProvider = TimeProvider.System;

        var userId = Guid.NewGuid();
        var notifId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = notifId,
            EventId = Guid.NewGuid(),
            ReminderRuleId = Guid.NewGuid(),
            RecipientUserId = userId,
            OriginalStartAtUtc = DateTimeOffset.UtcNow,
            EffectiveStartAtUtc = DateTimeOffset.UtcNow,
            ScheduledFireAtUtc = DateTimeOffset.UtcNow,
            RepeatIndex = 0,
            Title = "Alert",
            SentAtUtc = DateTimeOffset.UtcNow
        };
        repo.Notifications[notifId] = notification;

        var getUseCase = new GetNotificationsUseCase(repo);
        var markReadUseCase = new MarkNotificationReadUseCase(repo, timeProvider);

        var list = await getUseCase.GetNotificationsAsync(userId, default);
        Assert.Single(list);
        Assert.Null(list[0].ReadAt);

        // Mark read
        var marked1 = await markReadUseCase.MarkNotificationReadAsync(userId, notifId, default);
        Assert.True(marked1);
        Assert.NotNull(notification.ReadAtUtc);

        var readAtTime = notification.ReadAtUtc;

        // Idempotent mark read
        var marked2 = await markReadUseCase.MarkNotificationReadAsync(userId, notifId, default);
        Assert.True(marked2);
        Assert.Equal(readAtTime, notification.ReadAtUtc);

        // Not found
        Assert.False(await markReadUseCase.MarkNotificationReadAsync(userId, Guid.NewGuid(), default));
    }
}
