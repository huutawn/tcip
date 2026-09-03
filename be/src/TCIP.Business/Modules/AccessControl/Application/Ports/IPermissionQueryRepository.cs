namespace TCIP.Business.Modules.AccessControl.Application.Ports;

public interface IPermissionQueryRepository
{
    Task<IEnumerable<string>> GetPermissionsForPrincipalAsync(Guid principalId, Guid? resourceId = null, CancellationToken ct = default);
    Task<bool> HasPermissionAsync(Guid principalId, string permissionName, Guid? resourceId = null, CancellationToken ct = default);
}
