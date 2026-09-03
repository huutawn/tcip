using TCIP.Business.Modules.AccessControl.Application.Ports;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.Permissions;

public interface IDeletePermissionUseCase
{
    Task<bool> DeletePermissionAsync(Guid permissionId, CancellationToken cancellationToken = default);
}

public sealed class DeletePermissionUseCase(IPermissionRepository permissionRepository) : IDeletePermissionUseCase
{
    public async Task<bool> DeletePermissionAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await permissionRepository.GetPermissionByIdAsync(permissionId, cancellationToken);
        if (permission is null)
        {
            return false;
        }

        await permissionRepository.DeletePermissionAsync(permission, cancellationToken);
        return true;
    }
}
