using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Enums;

namespace TCIP.Business.Modules.Identity.Application.Ports;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPageAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<bool> UpdateRoleAsync(Guid id, UserRole role, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default);
}

public interface ISessionRepository
{
    Task<Session?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default);
    Task CreateAsync(Session session, CancellationToken cancellationToken = default);
    Task<bool> RotateAsync(Guid sessionId, string expectedRefreshTokenHash, string newRefreshTokenHash, DateTimeOffset rotatedAtUtc, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string hashedPassword, string providedPassword);
}

public interface ITokenIssuer
{
    Task<string> GenerateAccessTokenAsync(User user, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default);
}

public interface IRefreshTokenGenerator
{
    string Generate();
    string Hash(string token);
}

public interface IIdentityConfiguration
{
    int AccessTokenMinutes { get; }
    int RefreshTokenDays { get; }
}
