using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Api.Security;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases.Events;
using TCIP.Common.Exceptions;

namespace TCIP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendar/events")]
public sealed class EventController(
    ICreateEventUseCase createEventUseCase,
    IGetEventByIdUseCase getEventByIdUseCase,
    IUpdateEventUseCase updateEventUseCase,
    ICancelEventUseCase cancelEventUseCase,
    ISetAudienceUseCase setAudienceUseCase,
    IRemoveAudienceUseCase removeAudienceUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CalendarEventDetailResponse>> CreateEventAsync(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var result = await createEventUseCase.CreateEventAsync(request, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Created($"api/calendar/events/{result.Id}", result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CalendarEventDetailResponse>> GetEventByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var result = await getEventByIdUseCase.GetEventDetailAsync(id, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CalendarEventDetailResponse>> UpdateEventAsync(
        Guid id,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var result = await updateEventUseCase.UpdateEventAsync(id, request, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> CancelEventAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var version = await cancelEventUseCase.CancelEventAsync(id, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return NoContent();
    }

    [HttpPut("{id:guid}/audiences/{principalId:guid}")]
    public async Task<IActionResult> SetAudienceAsync(
        Guid id,
        Guid principalId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var version = await setAudienceUseCase.SetAudienceAsync(id, principalId, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return NoContent();
    }

    [HttpDelete("{id:guid}/audiences/{principalId:guid}")]
    public async Task<IActionResult> RemoveAudienceAsync(
        Guid id,
        Guid principalId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var version = await removeAudienceUseCase.RemoveAudienceAsync(id, principalId, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return NoContent();
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
