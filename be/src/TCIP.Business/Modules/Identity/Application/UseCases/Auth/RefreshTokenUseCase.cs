using TCIP.Business.Modules.Identity.Application.Contracts;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Identity.Application.UseCases.Auth;

public interface IRefreshTokenUseCase
{
    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}

public sealed class RefreshTokenUseCase(
    ISessionRepository sessionRepository,
    ITokenIssuer tokenIssuer,
    IRefreshTokenGenerator refreshTokenGenerator,
    IIdentityConfiguration configuration,
    TimeProvider timeProvider) : IRefreshTokenUseCase
{
    public async Task<LoginResponse> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var refreshTokenHash = refreshTokenGenerator.Hash(request.RefreshToken);

        var session = await sessionRepository.GetByRefreshTokenHashAsync(refreshTokenHash, cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (session is null || session.RevokedAtUtc is not null || session.ExpiresAtUtc <= now)
        {
            throw new UnauthenticationException("Invalid refresh token.");
        }

        var accessExpiresAt = now.AddMinutes(configuration.AccessTokenMinutes);
        var refreshExpiresAt = now.AddDays(configuration.RefreshTokenDays);
        var newRefreshToken = refreshTokenGenerator.Generate();

        var accessToken = await tokenIssuer.GenerateAccessTokenAsync(session.User, accessExpiresAt, cancellationToken);
        var newRefreshTokenHash = refreshTokenGenerator.Hash(newRefreshToken);

        var rotated = await sessionRepository.RotateAsync(
            session.Id,
            refreshTokenHash,
            newRefreshTokenHash,
            now,
            refreshExpiresAt,
            cancellationToken);

        if (!rotated)
        {
            throw new UnauthenticationException("Invalid refresh token.");
        }

        return new LoginResponse(
            accessToken,
            accessExpiresAt,
            newRefreshToken,
            refreshExpiresAt);
    }
}
