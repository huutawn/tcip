using TCIP.Business.Modules.Identity.Application.Contracts;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Identity.Application.UseCases.Auth;

public interface ILoginUserUseCase
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public sealed class LoginUserUseCase(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer,
    IRefreshTokenGenerator refreshTokenGenerator,
    IIdentityConfiguration configuration,
    TimeProvider timeProvider) : ILoginUserUseCase
{
    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            throw new UnauthenticationException("Invalid credentials.");
        }

        if (!passwordHasher.VerifyPassword(user, user.PasswordHash, request.Password))
        {
            throw new UnauthenticationException("Invalid credentials.");
        }

        var now = timeProvider.GetUtcNow();
        var accessExpiresAt = now.AddMinutes(configuration.AccessTokenMinutes);
        var refreshExpiresAt = now.AddDays(configuration.RefreshTokenDays);
        var refreshToken = refreshTokenGenerator.Generate();

        var accessToken = await tokenIssuer.GenerateAccessTokenAsync(user, accessExpiresAt, cancellationToken);
        var refreshTokenHash = refreshTokenGenerator.Hash(refreshToken);

        await sessionRepository.CreateAsync(
            new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RefreshTokenHash = refreshTokenHash,
                CreatedAtUtc = now,
                ExpiresAtUtc = refreshExpiresAt
            },
            cancellationToken);

        return new LoginResponse(
            accessToken,
            accessExpiresAt,
            refreshToken,
            refreshExpiresAt);
    }
}
