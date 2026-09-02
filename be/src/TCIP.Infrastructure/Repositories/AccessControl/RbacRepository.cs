using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Business.Modules.Identity.Domain.Enums;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.AccessControl;

public sealed class RbacRepository(TcipDbContext dbContext) : IRbacRepository
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

    private IQueryable<Principal> PrincipalDetailsQuery() =>
        dbContext.Principals
            .Include(x => x.User)
            .Include(x => x.Group)
            .Include(x => x.Team)
            .Include(x => x.Project)
            .Include(x => x.Department);

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

    public async Task<RoleAssignment> CreateRoleAssignmentAsync(RoleAssignment item, CancellationToken ct = default)
    {
        dbContext.RoleAssignments.Add(item);
        await dbContext.SaveChangesAsync(ct);
        return item;
    }

    public Task<RoleAssignment?> GetRoleAssignmentByIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.RoleAssignments.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<RoleAssignment?> GetRoleAssignmentAsync(Guid subjectId, Guid roleId, Guid? resourceId, CancellationToken ct = default) =>
        dbContext.RoleAssignments.Include(x => x.Role).FirstOrDefaultAsync(x => x.SubjectPrincipalId == subjectId && x.RoleId == roleId && x.ResourcePrincipalId == resourceId, ct);

    public async Task<IEnumerable<Role>> GetRolesByPrincipalIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.RoleAssignments.Where(x => x.SubjectPrincipalId == id).Select(x => x.Role).Distinct().ToListAsync(ct);

    public async Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByPrincipalIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.RoleAssignments.Include(x => x.Role).Where(x => x.SubjectPrincipalId == id).AsNoTracking().ToListAsync(ct);

    public async Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByRoleIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.RoleAssignments.Include(x => x.Role).Where(x => x.RoleId == id).AsNoTracking().ToListAsync(ct);

    public async Task DeleteRoleAssignmentAsync(RoleAssignment item, CancellationToken ct = default)
    {
        dbContext.RoleAssignments.Remove(item);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteRoleAssignmentsAsync(Guid subjectId, Guid? resourceId, CancellationToken ct = default)
    {
        var items = await dbContext.RoleAssignments.Where(x => x.SubjectPrincipalId == subjectId && x.ResourcePrincipalId == resourceId).ToListAsync(ct);
        if (items.Count == 0) return false;
        dbContext.RoleAssignments.RemoveRange(items);
        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IEnumerable<string>> GetPermissionsForPrincipalAsync(Guid principalId, Guid? resourceId = null, CancellationToken ct = default)
    {
        var principal = await dbContext.Principals.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == principalId && x.Available, ct);
        if (principal is null) return [];
        var subjectId = principal.User?.PrincipalId ?? principalId;
        if (principal.User?.Role == UserRole.Admin) return await dbContext.Permissions.Select(x => x.Name).ToListAsync(ct);
        if (resourceId.HasValue)
        {
            var membership = await dbContext.PrincipalMemberships.FirstOrDefaultAsync(x => x.UserId == principal.User!.Id && x.PrincipalId == resourceId.Value && x.LeftAtUtc == null, ct);
            if (membership?.IsOwner == true) return await dbContext.Permissions.Select(x => x.Name).ToListAsync(ct);
            if (membership is null)
            {
                resourceId = null;
            }
        }
        var roleIds = dbContext.RoleAssignments.Where(x => x.SubjectPrincipalId == subjectId && (x.ResourcePrincipalId == null || x.ResourcePrincipalId == resourceId)).Select(x => x.RoleId);
        var roles = dbContext.RolePermissions.Where(x => roleIds.Contains(x.RoleId)).Select(x => x.Permission.Name);
        var direct = dbContext.PermissionGrants.Where(x => x.SubjectPrincipalId == subjectId && (x.ResourcePrincipalId == null || x.ResourcePrincipalId == resourceId)).Select(x => x.Permission.Name);
        return await roles.Concat(direct).Distinct().ToListAsync(ct);
    }

    public async Task<bool> HasPermissionAsync(Guid principalId, string permissionName, Guid? resourceId = null, CancellationToken ct = default) =>
        (await GetPermissionsForPrincipalAsync(principalId, resourceId, ct)).Any(x => x.Equals(permissionName, StringComparison.OrdinalIgnoreCase));

    public async Task<AuthorizationSnapshot> GetAuthorizationSnapshotAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found.");
        var isGlobalAdmin = user.Role == UserRole.Admin || await dbContext.RoleAssignments.AnyAsync(x => x.SubjectPrincipalId == user.PrincipalId && x.ResourcePrincipalId == null && x.Role.Name.ToLower() == BuiltInRbacCatalog.AdminRole.ToLower(), ct);
        if (isGlobalAdmin)
            return new AuthorizationSnapshot([], [], [], true);

        var globalRoleIds = dbContext.RoleAssignments
            .Where(x => x.SubjectPrincipalId == user.PrincipalId && x.ResourcePrincipalId == null)
            .Select(x => x.RoleId);
        var globalPermissions = await dbContext.RolePermissions
            .Where(x => globalRoleIds.Contains(x.RoleId))
            .Select(x => x.Permission.Name)
            .Concat(dbContext.PermissionGrants.Where(x => x.SubjectPrincipalId == user.PrincipalId && x.ResourcePrincipalId == null).Select(x => x.Permission.Name))
            .Distinct()
            .ToListAsync(ct);

        var memberships = await dbContext.PrincipalMemberships.AsNoTracking()
            .Where(x => x.UserId == userId && x.LeftAtUtc == null)
            .Select(x => new { x.PrincipalId, x.IsOwner })
            .ToListAsync(ct);
        var resourceIds = memberships.Select(x => x.PrincipalId).ToArray();
        if (resourceIds.Length == 0)
            return new AuthorizationSnapshot(globalPermissions, [], [], false);

        var roleAssignments = await dbContext.RoleAssignments
            .Where(x => x.SubjectPrincipalId == user.PrincipalId && x.ResourcePrincipalId.HasValue && resourceIds.Contains(x.ResourcePrincipalId.Value))
            .SelectMany(x => x.Role.RolePermissions.Select(rp => new { ResourcePrincipalId = x.ResourcePrincipalId!.Value, Permission = rp.Permission.Name }))
            .ToListAsync(ct);
        var directPermissions = await dbContext.PermissionGrants
            .Where(x => x.SubjectPrincipalId == user.PrincipalId && x.ResourcePrincipalId.HasValue && resourceIds.Contains(x.ResourcePrincipalId.Value))
            .Select(x => new { ResourcePrincipalId = x.ResourcePrincipalId!.Value, Permission = x.Permission.Name })
            .ToListAsync(ct);
        var resourcePermissions = roleAssignments.Concat(directPermissions)
            .GroupBy(x => x.ResourcePrincipalId)
            .Select(x => new ResourcePermissionSnapshot(x.Key, x.Select(y => y.Permission).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()))
            .ToArray();
        return new AuthorizationSnapshot(globalPermissions, resourcePermissions, memberships.Where(x => x.IsOwner).Select(x => x.PrincipalId).ToArray(), false);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => dbContext.SaveChangesAsync(ct);
}
