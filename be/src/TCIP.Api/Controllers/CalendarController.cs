using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Api.Security;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases;
using TCIP.Common.Exceptions;

namespace TCIP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendar")]
public sealed class CalendarController(
    IEventCommandUseCase eventCommandUseCase,
    IReminderRuleUseCase reminderRuleUseCase,
    IEventOccurrenceUseCase eventOccurrenceUseCase,
    INotificationQueryUseCase notificationQueryUseCase) : ControllerBase
{
    [HttpPost("events")]
    public async Task<ActionResult<CalendarEventDetailResponse>> CreateEventAsync(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var result = await eventCommandUseCase.CreateEventAsync(request, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Created($"api/calendar/events/{result.Id}", result);
    }

    [HttpGet("events/{id:guid}")]
    public async Task<ActionResult<CalendarEventDetailResponse>> GetEventByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var result = await eventCommandUseCase.GetEventDetailAsync(id, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPut("events/{id:guid}")]
    public async Task<ActionResult<CalendarEventDetailResponse>> UpdateEventAsync(
        Guid id,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var result = await eventCommandUseCase.UpdateEventAsync(id, request, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpDelete("events/{id:guid}")]
    public async Task<IActionResult> CancelEventAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var version = await eventCommandUseCase.CancelEventAsync(id, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return NoContent();
    }

    [HttpPut("events/{id:guid}/audiences/{principalId:guid}")]
    public async Task<IActionResult> SetAudienceAsync(
        Guid id,
        Guid principalId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var version = await eventCommandUseCase.SetAudienceAsync(id, principalId, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return NoContent();
    }

    [HttpDelete("events/{id:guid}/audiences/{principalId:guid}")]
    public async Task<IActionResult> RemoveAudienceAsync(
        Guid id,
        Guid principalId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var version = await eventCommandUseCase.RemoveAudienceAsync(id, principalId, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return NoContent();
    }

    [HttpPost("events/{id:guid}/reminder-rules")]
    public async Task<ActionResult<ReminderRuleResponse>> AddReminderRuleAsync(
        Guid id,
        [FromBody] CreateReminderRuleRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var (response, version) = await reminderRuleUseCase.AddReminderRuleAsync(id, request, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return Created($"api/calendar/events/{id}/reminder-rules/{response.Id}", response);
    }

    [HttpPut("events/{id:guid}/reminder-rules/{ruleId:guid}")]
    public async Task<ActionResult<ReminderRuleResponse>> UpdateReminderRuleAsync(
        Guid id,
        Guid ruleId,
        [FromBody] UpdateReminderRuleRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var (response, version) = await reminderRuleUseCase.UpdateReminderRuleAsync(id, ruleId, request, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return Ok(response);
    }

    [HttpDelete("events/{id:guid}/reminder-rules/{ruleId:guid}")]
    public async Task<IActionResult> DeleteReminderRuleAsync(
        Guid id,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var version = await reminderRuleUseCase.DeleteReminderRuleAsync(id, ruleId, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return NoContent();
    }

    [HttpPut("events/{id:guid}/occurrence-exceptions")]
    public async Task<ActionResult<OccurrenceExceptionResponse>> UpsertOccurrenceExceptionAsync(
        Guid id,
        [FromBody] UpsertOccurrenceExceptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var (response, version) = await eventOccurrenceUseCase.UpsertOccurrenceExceptionAsync(id, request, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return Ok(response);
    }

    [HttpDelete("events/{id:guid}/occurrence-exceptions")]
    public async Task<IActionResult> DeleteOccurrenceExceptionAsync(
        Guid id,
        [FromQuery] DateTimeOffset originalStartAtUtc,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var version = await eventOccurrenceUseCase.DeleteOccurrenceExceptionAsync(id, originalStartAtUtc, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return NoContent();
    }

    [HttpGet("events/by-day")]
    public async Task<ActionResult<CalendarEventsByDayResponse>> GetEventsByDayAsync(
        [FromQuery] DateTimeOffset? day,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var result = await eventOccurrenceUseCase.GetEventsByDayAsync(userId, day, cursor, limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> GetNotificationsAsync(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var list = await notificationQueryUseCase.GetNotificationsAsync(userId, cancellationToken);
        return Ok(list);
    }

    [HttpPut("notifications/{notificationId:guid}/read")]
    public async Task<IActionResult> MarkNotificationReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        return await notificationQueryUseCase.MarkNotificationReadAsync(userId, notificationId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private static long ParseStrongETag(string? ifMatchHeader)
    {
        if (string.IsNullOrWhiteSpace(ifMatchHeader))
        {
            throw new PreconditionRequiredException("Precondition Required: If-Match header is required.");
        }

        var trimmed = ifMatchHeader.Trim();
        if (!trimmed.StartsWith('"') || !trimmed.EndsWith('"') || trimmed.Length < 3)
        {
            throw new PreconditionFailedException($"Precondition Failed: Invalid or malformed ETag '{ifMatchHeader}'.");
        }

        var inner = trimmed[1..^1];
        if (!long.TryParse(inner, out var version) || version < 1)
        {
            throw new PreconditionFailedException($"Precondition Failed: Invalid ETag version '{ifMatchHeader}'.");
        }

        return version;
    }
}
