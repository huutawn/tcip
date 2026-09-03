using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.Roles;

public interface IGetAllRolesUseCase
{
    Task<IEnumerable<RoleResponse>> GetAllRolesAsync(CancellationToken cancellationToken = default);
}

public sealed class GetAllRolesUseCase(IRoleRepository roleRepository) : IGetAllRolesUseCase
{
    public async Task<IEnumerable<RoleResponse>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await roleRepository.GetAllRolesAsync(cancellationToken);
        return roles.Select(r => new RoleResponse(
            r.Id,
            r.Name,
            r.Description,
            r.RolePermissions.Select(rp => new PermissionResponse(rp.PermissionId, rp.Permission.Name, rp.Permission.Description)).ToList()
        ));
    }
}
