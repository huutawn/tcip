using TCIP.Business.Modules.Identity.Application.Contracts;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Enums;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Identity.Application.UseCases;

public interface IAuthUseCase
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}

public sealed class AuthUseCase(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer,
    IRefreshTokenGenerator refreshTokenGenerator,
    IIdentityConfiguration configuration,
    TimeProvider timeProvider) : IAuthUseCase
{
    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await userRepository.ExistsByEmailAsync(email, cancellationToken);
        if (exists)
        {
            throw new ConflictException("Email already exists.");
        }

        var now = timeProvider.GetUtcNow();
        var principalId = Guid.NewGuid();

        var user = new User
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal
            {
                Id = principalId,
                Type = PrincipalType.User
            },
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            EmailVerified = false,
            Role = UserRole.User,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await userRepository.CreateAsync(user, cancellationToken);

        return new RegisterResponse(
            user.Id,
            user.PrincipalId,
            user.Email,
            user.DisplayName,
            user.CreatedAtUtc);
    }

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
        var issuedTokens = await IssueTokensAsync(user, now, cancellationToken);

        await sessionRepository.CreateAsync(
            new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RefreshTokenHash = issuedTokens.RefreshTokenHash,
                CreatedAtUtc = now,
                ExpiresAtUtc = issuedTokens.Response.RefreshTokenExpiresAtUtc
            },
            cancellationToken);

        return issuedTokens.Response;
    }

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

        var issuedTokens = await IssueTokensAsync(session.User, now, cancellationToken);

        var rotated = await sessionRepository.RotateAsync(
            session.Id,
            refreshTokenHash,
            issuedTokens.RefreshTokenHash,
            now,
            issuedTokens.Response.RefreshTokenExpiresAtUtc,
            cancellationToken);

        if (!rotated)
        {
            throw new UnauthenticationException("Invalid refresh token.");
        }

        return issuedTokens.Response;
    }

    private async Task<(LoginResponse Response, string RefreshTokenHash)> IssueTokensAsync(
        User user,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accessExpiresAt = now.AddMinutes(configuration.AccessTokenMinutes);
        var refreshExpiresAt = now.AddDays(configuration.RefreshTokenDays);
        var refreshToken = refreshTokenGenerator.Generate();

        var accessToken = await tokenIssuer.GenerateAccessTokenAsync(user, accessExpiresAt, cancellationToken);

        return (
            new LoginResponse(
                accessToken,
                accessExpiresAt,
                refreshToken,
                refreshExpiresAt),
            refreshTokenGenerator.Hash(refreshToken));
    }
}
