using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.Identity;

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
