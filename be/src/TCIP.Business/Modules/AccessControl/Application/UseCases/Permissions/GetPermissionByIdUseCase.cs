using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.Permissions;

public interface IGetPermissionByIdUseCase
{
    Task<PermissionResponse?> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
}

public sealed class GetPermissionByIdUseCase(IPermissionRepository permissionRepository) : IGetPermissionByIdUseCase
{
    public async Task<PermissionResponse?> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await permissionRepository.GetPermissionByIdAsync(permissionId, cancellationToken);
        return permission is null ? null : new PermissionResponse(permission.Id, permission.Name, permission.Description);
    }
}
