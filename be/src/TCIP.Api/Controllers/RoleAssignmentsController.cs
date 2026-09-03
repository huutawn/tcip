using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.UseCases.RoleAssignments;
using TCIP.Business.Modules.Identity.Domain.Enums;

namespace TCIP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rbac")]
public sealed class RoleAssignmentsController(
    ICreateRoleAssignmentUseCase createRoleAssignmentUseCase,
    IGetRoleAssignmentsByPrincipalIdUseCase getRoleAssignmentsByPrincipalIdUseCase,
    IGetRoleAssignmentsByRoleIdUseCase getRoleAssignmentsByRoleIdUseCase,
    IDeleteRoleAssignmentUseCase deleteRoleAssignmentUseCase) : ControllerBase
{
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("assignments")]
    public async Task<ActionResult<RoleAssignmentResponse>> CreateRoleAssignment(
        [FromBody] CreateRoleAssignmentReq request,
        CancellationToken cancellationToken)
    {
        var result = await createRoleAssignmentUseCase.CreateRoleAssignmentAsync(request, cancellationToken);
        return Created($"api/rbac/assignments/{result.Id}", result);
    }

    [HttpGet("principals/{principalId:guid}/assignments")]
    public async Task<ActionResult<IEnumerable<RoleAssignmentResponse>>> GetAssignmentsByPrincipal(
        Guid principalId,
        CancellationToken cancellationToken)
    {
        var result = await getRoleAssignmentsByPrincipalIdUseCase.GetRoleAssignmentsByPrincipalIdAsync(principalId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("roles/{roleId:guid}/assignments")]
    public async Task<ActionResult<IEnumerable<RoleAssignmentResponse>>> GetAssignmentsByRole(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var result = await getRoleAssignmentsByRoleIdUseCase.GetRoleAssignmentsByRoleIdAsync(roleId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> DeleteRoleAssignment(Guid assignmentId, CancellationToken cancellationToken)
    {
        var deleted = await deleteRoleAssignmentUseCase.DeleteRoleAssignmentAsync(assignmentId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
