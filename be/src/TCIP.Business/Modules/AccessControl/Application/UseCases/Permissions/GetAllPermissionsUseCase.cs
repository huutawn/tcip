using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.Permissions;

public interface IGetAllPermissionsUseCase
{
    Task<IEnumerable<PermissionResponse>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
}

public sealed class GetAllPermissionsUseCase(IPermissionRepository permissionRepository) : IGetAllPermissionsUseCase
{
    public async Task<IEnumerable<PermissionResponse>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await permissionRepository.GetAllPermissionsAsync(cancellationToken);
        return permissions.Select(p => new PermissionResponse(p.Id, p.Name, p.Description));
    }
}
