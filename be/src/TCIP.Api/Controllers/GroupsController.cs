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
[Route("api/groups")]
public sealed class GroupsController(
    IGroupUseCase groupUseCase,
    IMembershipUseCase membershipUseCase) : ControllerBase
{
    [PermissionAuthorize(Permissions.GroupCreate)]
    [HttpPost]
    public async Task<ActionResult<GroupResponse>> CreateAsync(
        CreateGroupReq request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId)) return Unauthorized();
        var group = await groupUseCase.CreateAsync(request, actorUserId, cancellationToken);
        return Created($"api/groups/{group.Id}", group);
    }

    [PermissionAuthorize(Permissions.MembershipManage, ResourceRoute = "groupId", ResourceType = PrincipalType.Group)]
    [HttpGet("{groupId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid groupId, CancellationToken cancellationToken) =>
        (await membershipUseCase.GetMembersAsync(PrincipalType.Group, groupId, cancellationToken)) is { } members
            ? Ok(members)
            : NotFound();

    [PermissionAuthorize(Permissions.MembershipManage, ResourceRoute = "groupId", ResourceType = PrincipalType.Group)]
    [HttpPut("{groupId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> SetMemberAsync(
        Guid groupId,
        Guid userId,
        SetMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId)) return Unauthorized();
        return await membershipUseCase.SetMemberAsync(PrincipalType.Group, groupId, actorUserId, userId, request, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
