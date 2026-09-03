using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Api.Security;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases.Occurrences;
using TCIP.Common.Exceptions;

namespace TCIP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendar")]
public sealed class OccurrenceController(
    IUpsertOccurrenceExceptionUseCase upsertOccurrenceExceptionUseCase,
    IDeleteOccurrenceExceptionUseCase deleteOccurrenceExceptionUseCase,
    IGetEventsByDayUseCase getEventsByDayUseCase) : ControllerBase
{
    [HttpPut("events/{id:guid}/occurrence-exceptions")]
    public async Task<ActionResult<OccurrenceExceptionResponse>> UpsertOccurrenceExceptionAsync(
        Guid id,
        [FromBody] UpsertOccurrenceExceptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var (response, version) = await upsertOccurrenceExceptionUseCase.UpsertOccurrenceExceptionAsync(id, request, expectedVersion, actorUserId, cancellationToken);
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
        var version = await deleteOccurrenceExceptionUseCase.DeleteOccurrenceExceptionAsync(id, originalStartAtUtc, expectedVersion, actorUserId, cancellationToken);
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

        var result = await getEventsByDayUseCase.GetEventsByDayAsync(userId, day, cursor, limit, cancellationToken);
        return Ok(result);
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
