using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Api.Security;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.UseCases.Memberships;
using TCIP.Business.Modules.Directory.Application.UseCases.Projects;
using TCIP.Business.Modules.Directory.Domain.Enums;

namespace TCIP.Api.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(
    ICreateProjectUseCase createProjectUseCase,
    IGetProjectByIdUseCase getProjectByIdUseCase,
    IUpdateProjectUseCase updateProjectUseCase,
    IDeleteProjectUseCase deleteProjectUseCase,
    IGetMembersUseCase getMembersUseCase,
    ISetMemberUseCase setMemberUseCase) : ControllerBase
{
    [PermissionAuthorize(Permissions.ProjectCreate)]
    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var ownerId))
        {
            return Unauthorized();
        }
        var project = await createProjectUseCase.CreateAsync(request, ownerId, cancellationToken);
        return Created($"api/projects/{project.Id}", project);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        (await getProjectByIdUseCase.GetByIdAsync(id, cancellationToken)) is { } project ? Ok(project) : NotFound();

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> Update(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken) =>
        (await updateProjectUseCase.UpdateAsync(id, request, cancellationToken)) is { } project ? Ok(project) : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await deleteProjectUseCase.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [PermissionAuthorize(Permissions.MembershipManage, ResourceRoute = "id", ResourceType = PrincipalType.Project)]
    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(
        Guid id,
        CancellationToken cancellationToken) =>
        (await getMembersUseCase.GetMembersAsync(PrincipalType.Project, id, cancellationToken)) is { } members
            ? Ok(members)
            : NotFound();

    [PermissionAuthorize(Permissions.MembershipManage, ResourceRoute = "id", ResourceType = PrincipalType.Project)]
    [HttpPut("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> SetMember(
        Guid id,
        Guid userId,
        SetMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId)) return Unauthorized();
        return await setMemberUseCase.SetMemberAsync(PrincipalType.Project, id, actorUserId, userId, request, cancellationToken) ? NoContent() : NotFound();
    }
}
