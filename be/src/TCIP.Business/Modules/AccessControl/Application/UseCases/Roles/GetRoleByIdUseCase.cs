using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.Roles;

public interface IGetRoleByIdUseCase
{
    Task<RoleResponse?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default);
}

public sealed class GetRoleByIdUseCase(IRoleRepository roleRepository) : IGetRoleByIdUseCase
{
    public async Task<RoleResponse?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await roleRepository.GetRoleByIdWithPermissionsAsync(roleId, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var permissions = role.RolePermissions
            .Select(rp => new PermissionResponse(rp.PermissionId, rp.Permission.Name, rp.Permission.Description))
            .ToList();

        return new RoleResponse(role.Id, role.Name, role.Description, permissions);
    }
}
