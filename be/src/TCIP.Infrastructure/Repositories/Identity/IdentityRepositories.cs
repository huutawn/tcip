using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Enums;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.Identity;

public sealed class UserRepository(TcipDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPageAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await dbContext.Users.CountAsync(cancellationToken);
        var items = await dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.CreatedAtUtc)
            .ThenBy(u => u.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> UpdateRoleAsync(
        Guid id,
        UserRole role,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsRelational())
        {
            var user = await dbContext.Users.FindAsync([id], cancellationToken);
            if (user is null) return false;
            user.Role = role;
            user.UpdatedAtUtc = updatedAtUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affectedRows = await dbContext.Users
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.Role, role)
                    .SetProperty(u => u.UpdatedAtUtc, updatedAtUtc),
                cancellationToken);

        return affectedRows == 1;
    }
}

public sealed class SessionRepository(TcipDbContext dbContext) : ISessionRepository
{
    public Task<Session?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Sessions
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.RefreshTokenHash == refreshTokenHash, cancellationToken);
    }

    public async Task CreateAsync(
        Session session,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Sessions.AddAsync(session, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RotateAsync(
        Guid sessionId,
        string expectedRefreshTokenHash,
        string newRefreshTokenHash,
        DateTimeOffset rotatedAtUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsRelational())
        {
            var session = await dbContext.Sessions.FirstOrDefaultAsync(
                x => x.Id == sessionId &&
                     x.RefreshTokenHash == expectedRefreshTokenHash &&
                     x.RevokedAtUtc == null &&
                     x.ExpiresAtUtc > rotatedAtUtc, cancellationToken);

            if (session is null) return false;

            session.RefreshTokenHash = newRefreshTokenHash;
            session.LastRotatedAtUtc = rotatedAtUtc;
            session.ExpiresAtUtc = expiresAtUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        var updated = await dbContext.Sessions
            .Where(x =>
                x.Id == sessionId &&
                x.RefreshTokenHash == expectedRefreshTokenHash &&
                x.RevokedAtUtc == null &&
                x.ExpiresAtUtc > rotatedAtUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RefreshTokenHash, newRefreshTokenHash)
                    .SetProperty(x => x.LastRotatedAtUtc, rotatedAtUtc)
                    .SetProperty(x => x.ExpiresAtUtc, expiresAtUtc),
                cancellationToken);

        return updated == 1;
    }
}
