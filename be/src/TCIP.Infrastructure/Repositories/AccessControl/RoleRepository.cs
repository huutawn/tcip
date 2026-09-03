using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.AccessControl;

public sealed class RoleRepository(TcipDbContext dbContext) : IRoleRepository
{
    public async Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default)
    {
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync(ct);
        return role;
    }

    public Task<Role?> GetRoleByIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Roles.FindAsync([id], ct).AsTask();

    public Task<Role?> GetRoleByIdWithPermissionsAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Roles
            .Include(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default) =>
        dbContext.Roles.FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower(), ct);

    public Task<bool> RoleExistsByNameAsync(string name, CancellationToken ct = default) =>
        dbContext.Roles.AnyAsync(x => x.Name.ToLower() == name.ToLower(), ct);

    public async Task<IEnumerable<Role>> GetAllRolesAsync(CancellationToken ct = default) =>
        await dbContext.Roles
            .Include(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task DeleteRoleAsync(Role role, CancellationToken ct = default)
    {
        dbContext.Roles.Remove(role);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<RolePermission> CreateRolePermissionAsync(RolePermission item, CancellationToken ct = default)
    {
        dbContext.RolePermissions.Add(item);
        await dbContext.SaveChangesAsync(ct);
        return item;
    }

    public Task<RolePermission?> GetRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct = default) =>
        dbContext.RolePermissions.FindAsync([roleId, permissionId], ct).AsTask();

    public async Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(Guid roleId, CancellationToken ct = default) =>
        await dbContext.RolePermissions.Where(x => x.RoleId == roleId).Select(x => x.Permission).ToListAsync(ct);

    public async Task<IEnumerable<RolePermission>> GetRolePermissionsByRoleIdAsync(Guid roleId, CancellationToken ct = default) =>
        await dbContext.RolePermissions.Include(x => x.Permission).Where(x => x.RoleId == roleId).ToListAsync(ct);

    public async Task SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid>? ids, CancellationToken ct = default)
    {
        dbContext.RolePermissions.RemoveRange(await dbContext.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(ct));
        if (ids != null)
        {
            await dbContext.RolePermissions.AddRangeAsync(ids.Distinct().Select(id => new RolePermission { RoleId = roleId, PermissionId = id }), ct);
        }
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteRolePermissionAsync(RolePermission item, CancellationToken ct = default)
    {
        dbContext.RolePermissions.Remove(item);
        await dbContext.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => dbContext.SaveChangesAsync(ct);
}
