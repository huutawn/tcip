using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Api.Security;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Application.UseCases.Reminders;
using TCIP.Common.Exceptions;

namespace TCIP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendar/events/{id:guid}/reminder-rules")]
public sealed class ReminderController(
    IAddReminderRuleUseCase addReminderRuleUseCase,
    IUpdateReminderRuleUseCase updateReminderRuleUseCase,
    IDeleteReminderRuleUseCase deleteReminderRuleUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReminderRuleResponse>> AddReminderRuleAsync(
        Guid id,
        [FromBody] CreateReminderRuleRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var (response, version) = await addReminderRuleUseCase.AddReminderRuleAsync(id, request, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return Created($"api/calendar/events/{id}/reminder-rules/{response.Id}", response);
    }

    [HttpPut("{ruleId:guid}")]
    public async Task<ActionResult<ReminderRuleResponse>> UpdateReminderRuleAsync(
        Guid id,
        Guid ruleId,
        [FromBody] UpdateReminderRuleRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var (response, version) = await updateReminderRuleUseCase.UpdateReminderRuleAsync(id, ruleId, request, expectedVersion, actorUserId, cancellationToken);
        Response.Headers.ETag = $"\"{version}\"";
        return Ok(response);
    }

    [HttpDelete("{ruleId:guid}")]
    public async Task<IActionResult> DeleteReminderRuleAsync(
        Guid id,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId))
            return Unauthorized();

        var expectedVersion = ParseStrongETag(Request.Headers.IfMatch.ToString());
        var version = await deleteReminderRuleUseCase.DeleteReminderRuleAsync(id, ruleId, expectedVersion, actorUserId, cancellationToken);
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
