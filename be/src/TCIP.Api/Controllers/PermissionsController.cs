using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.UseCases.Permissions;
using TCIP.Business.Modules.Identity.Domain.Enums;

namespace TCIP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rbac/permissions")]
public sealed class PermissionsController(
    ICreatePermissionUseCase createPermissionUseCase,
    IGetAllPermissionsUseCase getAllPermissionsUseCase,
    IGetPermissionByIdUseCase getPermissionByIdUseCase,
    IDeletePermissionUseCase deletePermissionUseCase) : ControllerBase
{
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public async Task<ActionResult<PermissionResponse>> CreatePermission(
        [FromBody] CreatePermissionReq request,
        CancellationToken cancellationToken)
    {
        var result = await createPermissionUseCase.CreatePermissionAsync(request, cancellationToken);
        return Created($"api/rbac/permissions/{result.Id}", result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionResponse>>> GetAllPermissions(CancellationToken cancellationToken)
    {
        var result = await getAllPermissionsUseCase.GetAllPermissionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PermissionResponse>> GetPermissionById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getPermissionByIdUseCase.GetPermissionByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePermission(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await deletePermissionUseCase.DeletePermissionAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
