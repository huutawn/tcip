using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Api.Security;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.UseCases;
using TCIP.Business.Modules.Directory.Domain.Enums;

namespace TCIP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/teams")]
public sealed class TeamsController(
    ITeamUseCase teamUseCase,
    IMembershipUseCase membershipUseCase) : ControllerBase
{
    [PermissionAuthorize(Permissions.TeamCreate)]
    [HttpPost]
    public async Task<ActionResult<TeamResponse>> Create(
        CreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId)) return Unauthorized();
        var team = await teamUseCase.CreateAsync(request, actorUserId, cancellationToken);
        return Created($"api/teams/{team.Id}", team);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeamResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        (await teamUseCase.GetByIdAsync(id, cancellationToken)) is { } team ? Ok(team) : NotFound();

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TeamResponse>> Update(
        Guid id,
        UpdateTeamRequest request,
        CancellationToken cancellationToken) =>
        (await teamUseCase.UpdateAsync(id, request, cancellationToken)) is { } team ? Ok(team) : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await teamUseCase.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [PermissionAuthorize(Permissions.MembershipManage, ResourceRoute = "id", ResourceType = PrincipalType.Team)]
    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken cancellationToken) =>
        (await membershipUseCase.GetMembersAsync(PrincipalType.Team, id, cancellationToken)) is { } members
            ? Ok(members)
            : NotFound();

    [PermissionAuthorize(Permissions.MembershipManage, ResourceRoute = "id", ResourceType = PrincipalType.Team)]
    [HttpPut("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> SetMember(
        Guid id,
        Guid userId,
        SetMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId)) return Unauthorized();
        return await membershipUseCase.SetMemberAsync(PrincipalType.Team, id, actorUserId, userId, request, cancellationToken) ? NoContent() : NotFound();
    }
}
