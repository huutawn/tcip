using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.AccessControl;

public sealed class PermissionRepository(TcipDbContext dbContext) : IPermissionRepository
{
    public async Task<Permission> CreatePermissionAsync(Permission permission, CancellationToken ct = default)
    {
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync(ct);
        return permission;
    }

    public Task<Permission?> GetPermissionByIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Permissions.FindAsync([id], ct).AsTask();

    public Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken ct = default) =>
        dbContext.Permissions.FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower(), ct);

    public Task<bool> PermissionExistsByNameAsync(string name, CancellationToken ct = default) =>
        dbContext.Permissions.AnyAsync(x => x.Name.ToLower() == name.ToLower(), ct);

    public async Task<IEnumerable<Permission>> GetAllPermissionsAsync(CancellationToken ct = default) =>
        await dbContext.Permissions.AsNoTracking().ToListAsync(ct);

    public async Task<IEnumerable<Permission>> GetPermissionsByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        await dbContext.Permissions.Where(x => ids.Contains(x.Id)).ToListAsync(ct);

    public async Task DeletePermissionAsync(Permission permission, CancellationToken ct = default)
    {
        dbContext.Permissions.Remove(permission);
        await dbContext.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => dbContext.SaveChangesAsync(ct);
}
