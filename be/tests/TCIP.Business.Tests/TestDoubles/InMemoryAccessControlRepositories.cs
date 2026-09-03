using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;

namespace TCIP.Business.Tests.TestDoubles;

public sealed class InMemoryPrincipalRepository : IPrincipalRepository, IPrincipalAvailabilityQuery
{
    public readonly Dictionary<Guid, Principal> Principals = new();

    public Task<Principal?> GetPrincipalByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Principals.GetValueOrDefault(id));

    public Task<Principal?> GetPrincipalDetailsByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Principals.GetValueOrDefault(id));

    public Task<IReadOnlyList<Principal>> SearchPrincipalsAsync(
        PrincipalType? type, string? search, Guid? cursor, int limit, bool? available, CancellationToken ct = default)
    {
        var items = Principals.Values
            .Where(p => !type.HasValue || p.Type == type.Value)
            .Where(p => !available.HasValue || p.Available == available.Value)
            .OrderBy(p => p.Id)
            .Take(limit + 1)
            .ToList();
        return Task.FromResult<IReadOnlyList<Principal>>(items);
    }

    public Task<bool> ArePrincipalsAvailableAsync(IReadOnlyCollection<Guid> principalIds, CancellationToken cancellationToken = default)
    {
        if (principalIds.Count == 0) return Task.FromResult(true);
        var count = principalIds.Distinct().Count(id => Principals.TryGetValue(id, out var p) && p.Available);
        return Task.FromResult(count == principalIds.Distinct().Count());
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class InMemoryPermissionRepository : IPermissionRepository
{
    public readonly Dictionary<Guid, Permission> Permissions = new();

    public Task<Permission> CreatePermissionAsync(Permission permission, CancellationToken ct = default)
    {
        Permissions[permission.Id] = permission;
        return Task.FromResult(permission);
    }

    public Task<Permission?> GetPermissionByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Permissions.GetValueOrDefault(id));

    public Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken ct = default) =>
        Task.FromResult(Permissions.Values.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> PermissionExistsByNameAsync(string name, CancellationToken ct = default) =>
        Task.FromResult(Permissions.Values.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

    public Task<IEnumerable<Permission>> GetAllPermissionsAsync(CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<Permission>>(Permissions.Values.ToList());

    public Task<IEnumerable<Permission>> GetPermissionsByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idSet = ids.ToHashSet();
        return Task.FromResult<IEnumerable<Permission>>(Permissions.Values.Where(p => idSet.Contains(p.Id)).ToList());
    }

    public Task DeletePermissionAsync(Permission permission, CancellationToken ct = default)
    {
        Permissions.Remove(permission.Id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class InMemoryRoleRepository : IRoleRepository
{
    public readonly Dictionary<Guid, Role> Roles = new();
    public readonly List<RolePermission> RolePermissions = new();

    public Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default)
    {
        Roles[role.Id] = role;
        return Task.FromResult(role);
    }

    public Task<Role?> GetRoleByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Roles.GetValueOrDefault(id));

    public Task<Role?> GetRoleByIdWithPermissionsAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Roles.GetValueOrDefault(id));

    public Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default) =>
        Task.FromResult(Roles.Values.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> RoleExistsByNameAsync(string name, CancellationToken ct = default) =>
        Task.FromResult(Roles.Values.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

    public Task<IEnumerable<Role>> GetAllRolesAsync(CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<Role>>(Roles.Values.ToList());

    public Task DeleteRoleAsync(Role role, CancellationToken ct = default)
    {
        Roles.Remove(role.Id);
        return Task.CompletedTask;
    }

    public Task<RolePermission> CreateRolePermissionAsync(RolePermission item, CancellationToken ct = default)
    {
        RolePermissions.Add(item);
        return Task.FromResult(item);
    }

    public Task<RolePermission?> GetRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct = default) =>
        Task.FromResult(RolePermissions.FirstOrDefault(rp => rp.RoleId == roleId && rp.PermissionId == permissionId));

    public Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(Guid roleId, CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<Permission>>(RolePermissions.Where(rp => rp.RoleId == roleId).Select(rp => rp.Permission).ToList());

    public Task<IEnumerable<RolePermission>> GetRolePermissionsByRoleIdAsync(Guid roleId, CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<RolePermission>>(RolePermissions.Where(rp => rp.RoleId == roleId).ToList());

    public Task SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid>? ids, CancellationToken ct = default)
    {
        RolePermissions.RemoveAll(rp => rp.RoleId == roleId);
        if (ids != null)
        {
            foreach (var id in ids.Distinct())
            {
                RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = id });
            }
        }
        return Task.CompletedTask;
    }

    public Task DeleteRolePermissionAsync(RolePermission item, CancellationToken ct = default)
    {
        RolePermissions.Remove(item);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class InMemoryRoleAssignmentRepository : IRoleAssignmentRepository
{
    public readonly Dictionary<Guid, RoleAssignment> Assignments = new();

    public Task<RoleAssignment> CreateRoleAssignmentAsync(RoleAssignment item, CancellationToken ct = default)
    {
        Assignments[item.Id] = item;
        return Task.FromResult(item);
    }

    public Task<RoleAssignment?> GetRoleAssignmentByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Assignments.GetValueOrDefault(id));

    public Task<RoleAssignment?> GetRoleAssignmentAsync(Guid subjectId, Guid roleId, Guid? resourceId, CancellationToken ct = default) =>
        Task.FromResult(Assignments.Values.FirstOrDefault(a => a.SubjectPrincipalId == subjectId && a.RoleId == roleId && a.ResourcePrincipalId == resourceId));

    public Task<IEnumerable<Role>> GetRolesByPrincipalIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<Role>>(Assignments.Values.Where(a => a.SubjectPrincipalId == id).Select(a => a.Role).Distinct().ToList());

    public Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByPrincipalIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<RoleAssignment>>(Assignments.Values.Where(a => a.SubjectPrincipalId == id).ToList());

    public Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByRoleIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<RoleAssignment>>(Assignments.Values.Where(a => a.RoleId == id).ToList());

    public Task DeleteRoleAssignmentAsync(RoleAssignment item, CancellationToken ct = default)
    {
        Assignments.Remove(item.Id);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteRoleAssignmentsAsync(Guid subjectId, Guid? resourceId, CancellationToken ct = default)
    {
        var toRemove = Assignments.Values.Where(a => a.SubjectPrincipalId == subjectId && a.ResourcePrincipalId == resourceId).ToList();
        foreach (var a in toRemove) Assignments.Remove(a.Id);
        return Task.FromResult(toRemove.Count > 0);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class InMemoryPermissionQueryRepository : IPermissionQueryRepository
{
    public readonly Dictionary<Guid, HashSet<string>> PrincipalPermissions = new();

    public Task<IEnumerable<string>> GetPermissionsForPrincipalAsync(Guid principalId, Guid? resourceId = null, CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<string>>(PrincipalPermissions.GetValueOrDefault(principalId) ?? []);

    public Task<bool> HasPermissionAsync(Guid principalId, string permissionName, Guid? resourceId = null, CancellationToken ct = default)
    {
        var has = PrincipalPermissions.TryGetValue(principalId, out var perms) && perms.Contains(permissionName);
        return Task.FromResult(has);
    }
}
