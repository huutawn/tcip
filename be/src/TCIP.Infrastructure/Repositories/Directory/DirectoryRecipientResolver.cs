using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.Directory;

public sealed class DirectoryRecipientResolver(TcipDbContext dbContext) : IDirectoryRecipientResolver, IAudienceRecipientResolver, IUserAudienceMembershipQuery
{
    public async Task<IReadOnlyList<Guid>> GetRecipientsForAudiencesAsync(
        IReadOnlyCollection<Guid> audiencePrincipalIds,
        DateTimeOffset resolvedAtUtc,
        Guid? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        if (audiencePrincipalIds.Count == 0)
            return [];

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT u.id
            FROM users u
            JOIN principals up ON u.principal_id = up.id
            WHERE up.available = TRUE
              AND (@cursor IS NULL OR u.id > @cursor)
              AND (
                u.principal_id = ANY(@audienceIds)
                OR
                EXISTS (
                  SELECT 1 FROM principals ap
                  JOIN principal_memberships pm ON pm.principal_id = ap.id
                  WHERE ap.id = ANY(@audienceIds)
                    AND ap.available = TRUE
                    AND pm.user_id = u.id
                    AND pm.joined_at_utc <= @resolvedAt
                    AND (pm.left_at_utc IS NULL OR pm.left_at_utc > @resolvedAt)
                )
              )
            ORDER BY u.id
            LIMIT @limit;
            """;

        cmd.Parameters.Add(new NpgsqlParameter("audienceIds", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = audiencePrincipalIds.ToArray() });
        cmd.Parameters.Add(new NpgsqlParameter("resolvedAt", NpgsqlDbType.TimestampTz) { Value = resolvedAtUtc });
        cmd.Parameters.Add(new NpgsqlParameter("cursor", NpgsqlDbType.Uuid) { Value = (object?)cursor ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("limit", NpgsqlDbType.Integer) { Value = limit });

        var results = new List<Guid>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetGuid(0));
        }

        return results;
    }

    public async Task<IReadOnlyList<Guid>> GetRecipientsForEventAsync(
        Guid eventId,
        DateTimeOffset resolvedAtUtc,
        Guid? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT u.id
            FROM users u
            JOIN principals up ON u.principal_id = up.id
            WHERE up.available = TRUE
              AND (@cursor IS NULL OR u.id > @cursor)
              AND (
                EXISTS (
                  SELECT 1 FROM event_audiences ea
                  WHERE ea.event_id = @eventId
                    AND ea.principal_id = u.principal_id
                    AND ea.status = 'Active'
                )
                OR
                EXISTS (
                  SELECT 1 FROM event_audiences ea
                  JOIN principals ap ON ea.principal_id = ap.id
                  JOIN principal_memberships pm ON pm.principal_id = ea.principal_id
                  WHERE ea.event_id = @eventId
                    AND ea.status = 'Active'
                    AND ap.available = TRUE
                    AND pm.user_id = u.id
                    AND pm.joined_at_utc <= @resolvedAt
                    AND (pm.left_at_utc IS NULL OR pm.left_at_utc > @resolvedAt)
                )
              )
            ORDER BY u.id
            LIMIT @limit;
            """;

        cmd.Parameters.Add(new NpgsqlParameter("eventId", NpgsqlDbType.Uuid) { Value = eventId });
        cmd.Parameters.Add(new NpgsqlParameter("resolvedAt", NpgsqlDbType.TimestampTz) { Value = resolvedAtUtc });
        cmd.Parameters.Add(new NpgsqlParameter("cursor", NpgsqlDbType.Uuid) { Value = (object?)cursor ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("limit", NpgsqlDbType.Integer) { Value = limit });

        var results = new List<Guid>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetGuid(0));
        }

        return results;
    }

    public async Task<IReadOnlyList<Guid>> GetActiveAudiencePrincipalIdsForUserAsync(
        Guid userId,
        DateTimeOffset atUtc,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PrincipalMemberships
            .AsNoTracking()
            .Where(pm => pm.UserId == userId && pm.JoinedAtUtc <= atUtc && (pm.LeftAtUtc == null || pm.LeftAtUtc > atUtc))
            .Select(pm => pm.PrincipalId)
            .ToListAsync(cancellationToken);
    }
}
