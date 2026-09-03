using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases.Events;
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

public sealed class AuthorizationTests
{
    private sealed record TestEnvironment(
        CreateEventUseCase CreateEvent,
        GetEventByIdUseCase GetEventById,
        UpdateEventUseCase UpdateEvent,
        CancelEventUseCase CancelEvent,
        SetAudienceUseCase SetAudience,
        RemoveAudienceUseCase RemoveAudience,
        AddReminderRuleUseCase AddReminderRule,
        UpsertOccurrenceExceptionUseCase UpsertOccurrenceException,
        DeleteOccurrenceExceptionUseCase DeleteOccurrenceException,
        InMemoryCalendarRepository Repo,
        User Creator,
        User Outsider,
        User AudienceMember);

    private static TestEnvironment CreateTestEnvironment()
    {
        var repo = new InMemoryCalendarRepository();
        var recurrence = new SimpleTestRecurrenceEngine();
        var planner = new ReminderSchedulePlanner(recurrence);
        var timeProvider = TimeProvider.System;

        var createEvent = new CreateEventUseCase(repo, recurrence, planner, repo, timeProvider);
        var getEventById = new GetEventByIdUseCase(repo, repo);
        var updateEvent = new UpdateEventUseCase(repo, recurrence, planner, timeProvider);
        var cancelEvent = new CancelEventUseCase(repo, timeProvider);
        var setAudience = new SetAudienceUseCase(repo, planner, repo, timeProvider);
        var removeAudience = new RemoveAudienceUseCase(repo, planner, timeProvider);
        var addReminderRule = new AddReminderRuleUseCase(repo, planner, timeProvider);
        var upsertOccurrenceException = new UpsertOccurrenceExceptionUseCase(repo, recurrence, planner, timeProvider);
        var deleteOccurrenceException = new DeleteOccurrenceExceptionUseCase(repo, planner, timeProvider);

        var creatorPrincipal = new Principal { Id = Guid.NewGuid(), Type = PrincipalType.User, Available = true };
        var creator = new User { Id = Guid.NewGuid(), PrincipalId = creatorPrincipal.Id, Email = "creator@test.com", DisplayName = "Creator", PasswordHash = "hash" };

        var outsiderPrincipal = new Principal { Id = Guid.NewGuid(), Type = PrincipalType.User, Available = true };
        var outsider = new User { Id = Guid.NewGuid(), PrincipalId = outsiderPrincipal.Id, Email = "outsider@test.com", DisplayName = "Outsider", PasswordHash = "hash" };

        var audiencePrincipal = new Principal { Id = Guid.NewGuid(), Type = PrincipalType.User, Available = true };
        var audienceMember = new User { Id = Guid.NewGuid(), PrincipalId = audiencePrincipal.Id, Email = "audience@test.com", DisplayName = "Audience", PasswordHash = "hash" };

        repo.Principals[creatorPrincipal.Id] = creatorPrincipal;
        repo.Principals[outsiderPrincipal.Id] = outsiderPrincipal;
        repo.Principals[audiencePrincipal.Id] = audiencePrincipal;

        repo.Users[creator.Id] = creator;
        repo.Users[outsider.Id] = outsider;
        repo.Users[audienceMember.Id] = audienceMember;

        return new TestEnvironment(
            createEvent,
            getEventById,
            updateEvent,
            cancelEvent,
            setAudience,
            removeAudience,
            addReminderRule,
            upsertOccurrenceException,
            deleteOccurrenceException,
            repo,
            creator,
            outsider,
            audienceMember);
    }

    [Fact]
    public async Task GetEventDetail_CreatorAndAudienceCanRead_OutsiderGetsNotFound()
    {
        var env = CreateTestEnvironment();

        var created = await env.CreateEvent.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Secret Event", null)],
            AudiencePrincipalIds = [env.AudienceMember.PrincipalId]
        }, env.Creator.Id, default);

        var creatorView = await env.GetEventById.GetEventDetailAsync(created.Id, env.Creator.Id, default);
        Assert.Equal(created.Id, creatorView.Id);

        var audienceView = await env.GetEventById.GetEventDetailAsync(created.Id, env.AudienceMember.Id, default);
        Assert.Equal(created.Id, audienceView.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            env.GetEventById.GetEventDetailAsync(created.Id, env.Outsider.Id, default));
    }

    [Fact]
    public async Task UpdateEvent_NonCreator_ThrowsForbiddenException()
    {
        var env = CreateTestEnvironment();

        var created = await env.CreateEvent.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, env.Creator.Id, default);

        var updateReq = new UpdateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 11, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Hacked Title", null)]
        };

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            env.UpdateEvent.UpdateEventAsync(created.Id, updateReq, 1L, env.Outsider.Id, default));
        Assert.Contains("creator", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelEvent_NonCreator_ThrowsForbiddenException()
    {
        var env = CreateTestEnvironment();

        var created = await env.CreateEvent.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, env.Creator.Id, default);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            env.CancelEvent.CancelEventAsync(created.Id, 1L, env.Outsider.Id, default));
    }

    [Fact]
    public async Task ManageAudience_NonCreator_ThrowsForbiddenException()
    {
        var env = CreateTestEnvironment();

        var created = await env.CreateEvent.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, env.Creator.Id, default);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            env.SetAudience.SetAudienceAsync(created.Id, env.AudienceMember.PrincipalId, 1L, env.Outsider.Id, default));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            env.RemoveAudience.RemoveAudienceAsync(created.Id, env.AudienceMember.PrincipalId, 1L, env.Outsider.Id, default));
    }

    [Fact]
    public async Task ManageReminderRules_NonCreator_ThrowsForbiddenException()
    {
        var env = CreateTestEnvironment();

        var created = await env.CreateEvent.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, env.Creator.Id, default);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            env.AddReminderRule.AddReminderRuleAsync(created.Id, new CreateReminderRuleRequest(15), 1L, env.Outsider.Id, default));
    }

    [Fact]
    public async Task ManageOccurrenceExceptions_NonCreator_ThrowsForbiddenException()
    {
        var env = CreateTestEnvironment();

        var start = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var created = await env.CreateEvent.CreateEventAsync(new CreateEventRequest
        {
            StartAt = start,
            RecurrenceRule = "RRULE:FREQ=DAILY",
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, env.Creator.Id, default);

        var req = new UpsertOccurrenceExceptionRequest(start, true);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            env.UpsertOccurrenceException.UpsertOccurrenceExceptionAsync(created.Id, req, 1L, env.Outsider.Id, default));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            env.DeleteOccurrenceException.DeleteOccurrenceExceptionAsync(created.Id, start, 1L, env.Outsider.Id, default));
    }
}
