using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCIP.Api.Security;
using TCIP.Business.Modules.Identity.Application.Contracts;
using TCIP.Business.Modules.Identity.Application.UseCases;
using TCIP.Business.Modules.Identity.Domain.Enums;

namespace TCIP.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UserController(IUserUseCase userUseCase) : ControllerBase
{
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<ActionResult<PagedUsersResponse>> GetPage(
        [FromQuery] UserListQuery query,
        CancellationToken cancellationToken = default)
    {
        return Ok(await userUseCase.GetPageAsync(query, cancellationToken));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse?>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userResponse = await userUseCase.GetByIdAsync(id, cancellationToken);
        if (userResponse is null)
        {
            return NotFound();
        }

        return Ok(userResponse);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse?>> GetMe(CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var userResponse = await userUseCase.GetByIdAsync(userId, cancellationToken);
        if (userResponse is null)
        {
            return NotFound();
        }

        return Ok(userResponse);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        var updated = await userUseCase.UpdateRoleAsync(
            actorUserId,
            id,
            request,
            cancellationToken);

        return updated ? NoContent() : NotFound();
    }
}
