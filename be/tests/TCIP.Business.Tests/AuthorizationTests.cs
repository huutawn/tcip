using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases;
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
    private static (EventCommandUseCase CommandService, ReminderRuleUseCase RuleService, EventOccurrenceUseCase OccurrenceService, InMemoryCalendarRepository Repo, User Creator, User Outsider, User AudienceMember) CreateTestEnvironment()
    {
        var repo = new InMemoryCalendarRepository();
        var recurrence = new SimpleTestRecurrenceEngine();
        var planner = new ReminderSchedulePlanner(recurrence);
        var timeProvider = TimeProvider.System;

        var commandService = new EventCommandUseCase(repo, recurrence, planner, timeProvider);
        var ruleService = new ReminderRuleUseCase(repo, planner, timeProvider);
        var occurrenceService = new EventOccurrenceUseCase(repo, recurrence, planner, timeProvider);

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

        return (commandService, ruleService, occurrenceService, repo, creator, outsider, audienceMember);
    }

    [Fact]
    public async Task GetEventDetail_CreatorAndAudienceCanRead_OutsiderGetsNotFound()
    {
        var (cmd, _, _, _, creator, outsider, audience) = CreateTestEnvironment();

        var created = await cmd.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Secret Event", null)],
            AudiencePrincipalIds = [audience.PrincipalId]
        }, creator.Id, default);

        var creatorView = await cmd.GetEventDetailAsync(created.Id, creator.Id, default);
        Assert.Equal(created.Id, creatorView.Id);

        var audienceView = await cmd.GetEventDetailAsync(created.Id, audience.Id, default);
        Assert.Equal(created.Id, audienceView.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            cmd.GetEventDetailAsync(created.Id, outsider.Id, default));
    }

    [Fact]
    public async Task UpdateEvent_NonCreator_ThrowsForbiddenException()
    {
        var (cmd, _, _, _, creator, outsider, _) = CreateTestEnvironment();

        var created = await cmd.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, creator.Id, default);

        var updateReq = new UpdateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 11, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Hacked Title", null)]
        };

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            cmd.UpdateEventAsync(created.Id, updateReq, 1L, outsider.Id, default));
        Assert.Contains("creator", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelEvent_NonCreator_ThrowsForbiddenException()
    {
        var (cmd, _, _, _, creator, outsider, _) = CreateTestEnvironment();

        var created = await cmd.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, creator.Id, default);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            cmd.CancelEventAsync(created.Id, 1L, outsider.Id, default));
    }

    [Fact]
    public async Task ManageAudience_NonCreator_ThrowsForbiddenException()
    {
        var (cmd, _, _, _, creator, outsider, audience) = CreateTestEnvironment();

        var created = await cmd.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, creator.Id, default);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            cmd.SetAudienceAsync(created.Id, audience.PrincipalId, 1L, outsider.Id, default));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            cmd.RemoveAudienceAsync(created.Id, audience.PrincipalId, 1L, outsider.Id, default));
    }

    [Fact]
    public async Task ManageReminderRules_NonCreator_ThrowsForbiddenException()
    {
        var (cmd, ruleService, _, _, creator, outsider, _) = CreateTestEnvironment();

        var created = await cmd.CreateEventAsync(new CreateEventRequest
        {
            StartAt = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, creator.Id, default);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            ruleService.AddReminderRuleAsync(created.Id, new CreateReminderRuleRequest(15), 1L, outsider.Id, default));
    }

    [Fact]
    public async Task ManageOccurrenceExceptions_NonCreator_ThrowsForbiddenException()
    {
        var (cmd, _, occService, _, creator, outsider, _) = CreateTestEnvironment();

        var start = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var created = await cmd.CreateEventAsync(new CreateEventRequest
        {
            StartAt = start,
            RecurrenceRule = "RRULE:FREQ=DAILY",
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        }, creator.Id, default);

        var req = new UpsertOccurrenceExceptionRequest(start, true);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            occService.UpsertOccurrenceExceptionAsync(created.Id, req, 1L, outsider.Id, default));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            occService.DeleteOccurrenceExceptionAsync(created.Id, start, 1L, outsider.Id, default));
    }
}
