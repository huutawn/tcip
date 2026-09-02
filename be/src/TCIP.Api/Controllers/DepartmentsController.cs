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
[Route("api/departments")]
public sealed class DepartmentsController(
    IDepartmentUseCase departmentUseCase,
    IMembershipUseCase membershipUseCase) : ControllerBase
{
    [PermissionAuthorize(Permissions.DepartmentCreate)]
    [HttpPost]
    public async Task<ActionResult<DepartmentResponse>> Create(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId)) return Unauthorized();
        var department = await departmentUseCase.CreateAsync(request, actorUserId, cancellationToken);
        return Created($"api/departments/{department.Id}", department);
    }

    [PermissionAuthorize(Permissions.DepartmentRead, ResourceRoute = "id", ResourceType = PrincipalType.Department)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DepartmentResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        (await departmentUseCase.GetByIdAsync(id, cancellationToken)) is { } department ? Ok(department) : NotFound();

    [PermissionAuthorize(Permissions.DepartmentUpdate, ResourceRoute = "id", ResourceType = PrincipalType.Department)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DepartmentResponse>> Update(
        Guid id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken) =>
        (await departmentUseCase.UpdateAsync(id, request, cancellationToken)) is { } department ? Ok(department) : NotFound();

    [PermissionAuthorize(Permissions.DepartmentDelete, ResourceRoute = "id", ResourceType = PrincipalType.Department)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await departmentUseCase.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [PermissionAuthorize(Permissions.MembershipRead, ResourceRoute = "id", ResourceType = PrincipalType.Department)]
    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken cancellationToken) =>
        (await membershipUseCase.GetMembersAsync(PrincipalType.Department, id, cancellationToken)) is { } members
            ? Ok(members)
            : NotFound();

    [PermissionAuthorize(Permissions.MembershipManage, ResourceRoute = "id", ResourceType = PrincipalType.Department)]
    [HttpPut("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> SetMember(
        Guid id,
        Guid userId,
        SetMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId)) return Unauthorized();
        return await membershipUseCase.SetMemberAsync(PrincipalType.Department, id, actorUserId, userId, request, cancellationToken) ? NoContent() : NotFound();
    }
}
