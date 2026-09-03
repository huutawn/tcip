using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.UseCases.PermissionQueries;
using TCIP.Business.Modules.AccessControl.Application.UseCases.Principals;
using TCIP.Business.Modules.Identity.Domain.Enums;

namespace TCIP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rbac/principals")]
public sealed class PrincipalsController(
    ISearchPrincipalsUseCase searchPrincipalsUseCase,
    IGetPrincipalByIdUseCase getPrincipalByIdUseCase,
    ISetPrincipalAvailabilityUseCase setPrincipalAvailabilityUseCase,
    IGetPermissionsForPrincipalUseCase getPermissionsForPrincipalUseCase,
    ICheckPermissionUseCase checkPermissionUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PrincipalSearchResponse>> SearchPrincipals(
        [FromQuery] PrincipalSearchQuery query,
        CancellationToken cancellationToken)
    {
        var result = await searchPrincipalsUseCase.SearchPrincipalsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PrincipalResponse>> GetPrincipalById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getPrincipalByIdUseCase.GetPrincipalByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPatch("{id:guid}/availability")]
    public async Task<IActionResult> SetPrincipalAvailability(
        Guid id,
        SetPrincipalAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        await setPrincipalAvailabilityUseCase.SetPrincipalAvailabilityAsync(id, request.Available, cancellationToken);
        return NoContent();
    }

    [HttpGet("{principalId:guid}/permissions")]
    public async Task<ActionResult<PrincipalPermissionsResponse>> GetPermissionsForPrincipal(
        Guid principalId,
        [FromQuery] Guid? resourcePrincipalId,
        CancellationToken cancellationToken)
    {
        var result = await getPermissionsForPrincipalUseCase.GetPermissionsForPrincipalAsync(principalId, resourcePrincipalId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{principalId:guid}/check-permission")]
    public async Task<ActionResult<CheckPermissionResponse>> CheckPermission(
        Guid principalId,
        [FromQuery] string permission,
        [FromQuery] Guid? resourcePrincipalId,
        CancellationToken cancellationToken)
    {
        var result = await checkPermissionUseCase.CheckPermissionAsync(principalId, permission, resourcePrincipalId, cancellationToken);
        return Ok(result);
    }
}
