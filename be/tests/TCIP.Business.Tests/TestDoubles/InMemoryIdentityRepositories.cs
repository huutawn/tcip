using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Enums;

namespace TCIP.Business.Tests.TestDoubles;

public sealed class InMemoryUserRepository : IUserRepository, IUserPrincipalLookupQuery
{
    public readonly Dictionary<Guid, User> Users = new();

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.Values.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.Values.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        Users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.GetValueOrDefault(id));

    public Task<(IReadOnlyList<User> Items, int TotalCount)> GetPageAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var items = Users.Values.Skip(skip).Take(take).ToList();
        return Task.FromResult<(IReadOnlyList<User>, int)>((items, Users.Count));
    }

    public Task<bool> UpdateRoleAsync(Guid id, UserRole role, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default)
    {
        if (Users.TryGetValue(id, out var user))
        {
            user.Role = role;
            user.UpdatedAtUtc = updatedAtUtc;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<UserPrincipalInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (Users.TryGetValue(userId, out var user))
        {
            return Task.FromResult<UserPrincipalInfo?>(new UserPrincipalInfo(user.Id, user.PrincipalId, user.TimeZoneId, user.Language));
        }
        return Task.FromResult<UserPrincipalInfo?>(null);
    }
}

public sealed class InMemorySessionRepository : ISessionRepository
{
    public readonly Dictionary<Guid, Session> Sessions = new();

    public Task<Session?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Sessions.Values.FirstOrDefault(s => s.RefreshTokenHash == refreshTokenHash));

    public Task CreateAsync(Session session, CancellationToken cancellationToken = default)
    {
        Sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task<bool> RotateAsync(Guid sessionId, string expectedRefreshTokenHash, string newRefreshTokenHash, DateTimeOffset rotatedAtUtc, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
    {
        if (Sessions.TryGetValue(sessionId, out var session) && session.RefreshTokenHash == expectedRefreshTokenHash && session.RevokedAtUtc == null && session.ExpiresAtUtc > rotatedAtUtc)
        {
            session.RefreshTokenHash = newRefreshTokenHash;
            session.LastRotatedAtUtc = rotatedAtUtc;
            session.ExpiresAtUtc = expiresAtUtc;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}

public sealed class SimplePasswordHasher : IPasswordHasher
{
    public string HashPassword(User user, string password) => $"hashed_{password}";
    public bool VerifyPassword(User user, string hashedPassword, string providedPassword) =>
        hashedPassword == $"hashed_{providedPassword}";
}

public sealed class SimpleTokenIssuer : ITokenIssuer
{
    public Task<string> GenerateAccessTokenAsync(User user, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult($"access_token_for_{user.Id}");
}

public sealed class SimpleRefreshTokenGenerator : IRefreshTokenGenerator
{
    public string Generate() => Guid.NewGuid().ToString("N");
    public string Hash(string token) => $"hash_{token}";
}

public sealed class SimpleIdentityConfiguration : IIdentityConfiguration
{
    public int AccessTokenMinutes => 15;
    public int RefreshTokenDays => 7;
}
