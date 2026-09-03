using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TCIP.Business.Modules.Identity.Application.Contracts;
using TCIP.Business.Modules.Identity.Application.UseCases.Auth;

namespace TCIP.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(
    IRegisterUserUseCase registerUserUseCase,
    ILoginUserUseCase loginUserUseCase,
    IRefreshTokenUseCase refreshTokenUseCase) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await registerUserUseCase.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await loginUserUseCase.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await refreshTokenUseCase.RefreshAsync(request, cancellationToken);
        return Ok(response);
    }
}
