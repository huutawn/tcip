using Microsoft.EntityFrameworkCore;
using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.AccessControl;

public sealed class PrincipalRepository(TcipDbContext dbContext) : IPrincipalRepository, IPrincipalAvailabilityQuery
{
    public Task<Principal?> GetPrincipalByIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Principals.FindAsync([id], ct).AsTask();

    public Task<Principal?> GetPrincipalDetailsByIdAsync(Guid id, CancellationToken ct = default) =>
        PrincipalDetailsQuery().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Principal>> SearchPrincipalsAsync(
        PrincipalType? type,
        string? search,
        Guid? cursor,
        int limit,
        bool? available,
        CancellationToken ct = default)
    {
        var query = PrincipalDetailsQuery();
        if (type.HasValue) query = query.Where(x => x.Type == type.Value);
        if (available.HasValue) query = query.Where(x => x.Available == available.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = search.Trim().ToLower();
            query = query.Where(p =>
                (p.User != null && (p.User.DisplayName.ToLower().Contains(pattern) || p.User.Email.ToLower().Contains(pattern))) ||
                (p.Group != null && p.Group.Name.ToLower().Contains(pattern)) ||
                (p.Team != null && p.Team.Name.ToLower().Contains(pattern)) ||
                (p.Project != null && p.Project.Name.ToLower().Contains(pattern)) ||
                (p.Department != null && p.Department.Name.ToLower().Contains(pattern)));
        }
        if (cursor.HasValue) query = query.Where(x => x.Id.CompareTo(cursor.Value) > 0);
        return await query.AsNoTracking().OrderBy(x => x.Id).Take(limit + 1).ToListAsync(ct);
    }

    public async Task<bool> ArePrincipalsAvailableAsync(IReadOnlyCollection<Guid> principalIds, CancellationToken cancellationToken = default)
    {
        if (principalIds.Count == 0)
            return true;

        var count = await dbContext.Principals.CountAsync(p => principalIds.Contains(p.Id) && p.Available, cancellationToken);
        return count == principalIds.Distinct().Count();
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => dbContext.SaveChangesAsync(ct);

    private IQueryable<Principal> PrincipalDetailsQuery() =>
        dbContext.Principals
            .Include(x => x.User)
            .Include(x => x.Group)
            .Include(x => x.Team)
            .Include(x => x.Project)
            .Include(x => x.Department);
}
