using TCIP.Business.Modules.AccessControl.Domain.Entities;

namespace TCIP.Business.Modules.AccessControl.Application.Ports;

public interface IPermissionRepository
{
    Task<Permission> CreatePermissionAsync(Permission permission, CancellationToken ct = default);
    Task<Permission?> GetPermissionByIdAsync(Guid id, CancellationToken ct = default);
    Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken ct = default);
    Task<bool> PermissionExistsByNameAsync(string name, CancellationToken ct = default);
    Task<IEnumerable<Permission>> GetAllPermissionsAsync(CancellationToken ct = default);
    Task<IEnumerable<Permission>> GetPermissionsByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task DeletePermissionAsync(Permission permission, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
