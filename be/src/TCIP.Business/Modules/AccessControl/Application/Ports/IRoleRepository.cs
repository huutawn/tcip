using TCIP.Business.Modules.AccessControl.Domain.Entities;

namespace TCIP.Business.Modules.AccessControl.Application.Ports;

public interface IRoleRepository
{
    Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default);
    Task<Role?> GetRoleByIdAsync(Guid id, CancellationToken ct = default);
    Task<Role?> GetRoleByIdWithPermissionsAsync(Guid id, CancellationToken ct = default);
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default);
    Task<bool> RoleExistsByNameAsync(string name, CancellationToken ct = default);
    Task<IEnumerable<Role>> GetAllRolesAsync(CancellationToken ct = default);
    Task DeleteRoleAsync(Role role, CancellationToken ct = default);

    Task<RolePermission> CreateRolePermissionAsync(RolePermission item, CancellationToken ct = default);
    Task<RolePermission?> GetRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct = default);
    Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(Guid roleId, CancellationToken ct = default);
    Task<IEnumerable<RolePermission>> GetRolePermissionsByRoleIdAsync(Guid roleId, CancellationToken ct = default);
    Task SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid>? ids, CancellationToken ct = default);
    Task DeleteRolePermissionAsync(RolePermission item, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
