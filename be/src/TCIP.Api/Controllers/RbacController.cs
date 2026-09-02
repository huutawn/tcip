using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.UseCases;
using TCIP.Business.Modules.Identity.Domain.Enums;

namespace TCIP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rbac")]
public sealed class RbacController(IRbacUseCase rbacUseCase) : ControllerBase
{
    // Principals
    [HttpGet("principals")]
    public async Task<ActionResult<PrincipalSearchResponse>> SearchPrincipals(
        [FromQuery] PrincipalSearchQuery query,
        CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.SearchPrincipalsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("principals/{id:guid}")]
    public async Task<ActionResult<PrincipalResponse>> GetPrincipalById(Guid id, CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.GetPrincipalByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPatch("principals/{id:guid}/availability")]
    public async Task<IActionResult> SetPrincipalAvailability(
        Guid id,
        SetPrincipalAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        await rbacUseCase.SetPrincipalAvailabilityAsync(id, request.Available, cancellationToken);
        return NoContent();
    }

    // Permissions
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("permissions")]
    public async Task<ActionResult<PermissionResponse>> CreatePermission(
        [FromBody] CreatePermissionReq request,
        CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.CreatePermissionAsync(request, cancellationToken);
        return Created($"api/rbac/permissions/{result.Id}", result);
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<IEnumerable<PermissionResponse>>> GetAllPermissions(CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.GetAllPermissionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("permissions/{id:guid}")]
    public async Task<ActionResult<PermissionResponse>> GetPermissionById(Guid id, CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.GetPermissionByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("permissions/{id:guid}")]
    public async Task<IActionResult> DeletePermission(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await rbacUseCase.DeletePermissionAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // Roles
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("roles")]
    public async Task<ActionResult<RoleResponse>> CreateRole(
        [FromBody] CreateRoleReq request,
        CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.CreateRoleAsync(request, cancellationToken);
        return Created($"api/rbac/roles/{result.Id}", result);
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<RoleResponse>>> GetAllRoles(CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.GetAllRolesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("roles/{id:guid}")]
    public async Task<ActionResult<RoleResponse>> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.GetRoleByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("roles/{id:guid}/permissions")]
    public async Task<ActionResult<RoleResponse>> AssignPermissionsToRole(
        Guid id,
        [FromBody] AssignPermissionsToRoleReq request,
        CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.AssignPermissionsToRoleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("roles/{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await rbacUseCase.DeleteRoleAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // Role Assignments
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("assignments")]
    public async Task<ActionResult<RoleAssignmentResponse>> CreateRoleAssignment(
        [FromBody] CreateRoleAssignmentReq request,
        CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.CreateRoleAssignmentAsync(request, cancellationToken);
        return Created($"api/rbac/assignments/{result.Id}", result);
    }

    [HttpGet("principals/{principalId:guid}/assignments")]
    public async Task<ActionResult<IEnumerable<RoleAssignmentResponse>>> GetAssignmentsByPrincipal(
        Guid principalId,
        CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.GetRoleAssignmentsByPrincipalIdAsync(principalId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("roles/{roleId:guid}/assignments")]
    public async Task<ActionResult<IEnumerable<RoleAssignmentResponse>>> GetAssignmentsByRole(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.GetRoleAssignmentsByRoleIdAsync(roleId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> DeleteRoleAssignment(Guid assignmentId, CancellationToken cancellationToken)
    {
        var deleted = await rbacUseCase.DeleteRoleAssignmentAsync(assignmentId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // Permission Checking / Introspection
    [HttpGet("principals/{principalId:guid}/permissions")]
    public async Task<ActionResult<PrincipalPermissionsResponse>> GetPermissionsForPrincipal(
        Guid principalId,
        [FromQuery] Guid? resourcePrincipalId,
        CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.GetPermissionsForPrincipalAsync(principalId, resourcePrincipalId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("principals/{principalId:guid}/check-permission")]
    public async Task<ActionResult<CheckPermissionResponse>> CheckPermission(
        Guid principalId,
        [FromQuery] string permission,
        [FromQuery] Guid? resourcePrincipalId,
        CancellationToken cancellationToken)
    {
        var result = await rbacUseCase.CheckPermissionAsync(principalId, permission, resourcePrincipalId, cancellationToken);
        return Ok(result);
    }
}
