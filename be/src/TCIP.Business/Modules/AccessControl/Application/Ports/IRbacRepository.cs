using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;

namespace TCIP.Business.Modules.AccessControl.Application.Ports;

public interface IRbacRepository
{
    Task<Principal?> GetPrincipalByIdAsync(Guid id, CancellationToken ct = default);
    Task<Principal?> GetPrincipalDetailsByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Principal>> SearchPrincipalsAsync(PrincipalType? type, string? search, Guid? cursor, int limit, bool? available, CancellationToken ct = default);

    Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default);
    Task<Role?> GetRoleByIdAsync(Guid id, CancellationToken ct = default);
    Task<Role?> GetRoleByIdWithPermissionsAsync(Guid id, CancellationToken ct = default);
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default);
    Task<bool> RoleExistsByNameAsync(string name, CancellationToken ct = default);
    Task<IEnumerable<Role>> GetAllRolesAsync(CancellationToken ct = default);
    Task DeleteRoleAsync(Role role, CancellationToken ct = default);

    Task<Permission> CreatePermissionAsync(Permission permission, CancellationToken ct = default);
    Task<Permission?> GetPermissionByIdAsync(Guid id, CancellationToken ct = default);
    Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken ct = default);
    Task<bool> PermissionExistsByNameAsync(string name, CancellationToken ct = default);
    Task<IEnumerable<Permission>> GetAllPermissionsAsync(CancellationToken ct = default);
    Task<IEnumerable<Permission>> GetPermissionsByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task DeletePermissionAsync(Permission permission, CancellationToken ct = default);

    Task<RolePermission> CreateRolePermissionAsync(RolePermission item, CancellationToken ct = default);
    Task<RolePermission?> GetRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct = default);
    Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(Guid roleId, CancellationToken ct = default);
    Task<IEnumerable<RolePermission>> GetRolePermissionsByRoleIdAsync(Guid roleId, CancellationToken ct = default);
    Task SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> ids, CancellationToken ct = default);
    Task DeleteRolePermissionAsync(RolePermission item, CancellationToken ct = default);

    Task<RoleAssignment> CreateRoleAssignmentAsync(RoleAssignment item, CancellationToken ct = default);
    Task<RoleAssignment?> GetRoleAssignmentByIdAsync(Guid id, CancellationToken ct = default);
    Task<RoleAssignment?> GetRoleAssignmentAsync(Guid subjectId, Guid roleId, Guid? resourceId, CancellationToken ct = default);
    Task<IEnumerable<Role>> GetRolesByPrincipalIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByPrincipalIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByRoleIdAsync(Guid id, CancellationToken ct = default);
    Task DeleteRoleAssignmentAsync(RoleAssignment item, CancellationToken ct = default);
    Task<bool> DeleteRoleAssignmentsAsync(Guid subjectId, Guid? resourceId, CancellationToken ct = default);

    Task<IEnumerable<string>> GetPermissionsForPrincipalAsync(Guid principalId, Guid? resourceId = null, CancellationToken ct = default);
    Task<bool> HasPermissionAsync(Guid principalId, string permissionName, Guid? resourceId = null, CancellationToken ct = default);
    Task<AuthorizationSnapshot> GetAuthorizationSnapshotAsync(Guid userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
