using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.UseCases.Roles;
using TCIP.Business.Modules.Identity.Domain.Enums;

namespace TCIP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rbac/roles")]
public sealed class RolesController(
    ICreateRoleUseCase createRoleUseCase,
    IGetAllRolesUseCase getAllRolesUseCase,
    IGetRoleByIdUseCase getRoleByIdUseCase,
    IAssignPermissionsToRoleUseCase assignPermissionsToRoleUseCase,
    IDeleteRoleUseCase deleteRoleUseCase) : ControllerBase
{
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public async Task<ActionResult<RoleResponse>> CreateRole(
        [FromBody] CreateRoleReq request,
        CancellationToken cancellationToken)
    {
        var result = await createRoleUseCase.CreateRoleAsync(request, cancellationToken);
        return Created($"api/rbac/roles/{result.Id}", result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleResponse>>> GetAllRoles(CancellationToken cancellationToken)
    {
        var result = await getAllRolesUseCase.GetAllRolesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleResponse>> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getRoleByIdUseCase.GetRoleByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:guid}/permissions")]
    public async Task<ActionResult<RoleResponse>> AssignPermissionsToRole(
        Guid id,
        [FromBody] AssignPermissionsToRoleReq request,
        CancellationToken cancellationToken)
    {
        var result = await assignPermissionsToRoleUseCase.AssignPermissionsToRoleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await deleteRoleUseCase.DeleteRoleAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
