using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.PermissionQueries;

public interface ICheckPermissionUseCase
{
    Task<CheckPermissionResponse> CheckPermissionAsync(Guid principalId, string permission, Guid? resourcePrincipalId, CancellationToken cancellationToken = default);
}

public sealed class CheckPermissionUseCase(IPermissionQueryRepository permissionQueryRepository) : ICheckPermissionUseCase
{
    public async Task<CheckPermissionResponse> CheckPermissionAsync(
        Guid principalId,
        string permission,
        Guid? resourcePrincipalId,
        CancellationToken cancellationToken = default)
    {
        var hasPermission = await permissionQueryRepository.HasPermissionAsync(principalId, permission, resourcePrincipalId, cancellationToken);
        return new CheckPermissionResponse(principalId, permission, resourcePrincipalId, hasPermission);
    }
}
