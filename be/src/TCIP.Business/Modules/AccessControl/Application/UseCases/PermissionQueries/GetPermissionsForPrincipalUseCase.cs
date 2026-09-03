using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.PermissionQueries;

public interface IGetPermissionsForPrincipalUseCase
{
    Task<PrincipalPermissionsResponse> GetPermissionsForPrincipalAsync(Guid principalId, Guid? resourcePrincipalId, CancellationToken cancellationToken = default);
}

public sealed class GetPermissionsForPrincipalUseCase(IPermissionQueryRepository permissionQueryRepository) : IGetPermissionsForPrincipalUseCase
{
    public async Task<PrincipalPermissionsResponse> GetPermissionsForPrincipalAsync(
        Guid principalId,
        Guid? resourcePrincipalId,
        CancellationToken cancellationToken = default)
    {
        var permissions = await permissionQueryRepository.GetPermissionsForPrincipalAsync(principalId, resourcePrincipalId, cancellationToken);
        return new PrincipalPermissionsResponse(principalId, resourcePrincipalId, permissions.ToList());
    }
}
