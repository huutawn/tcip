using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.Roles;

public interface IAssignPermissionsToRoleUseCase
{
    Task<RoleResponse> AssignPermissionsToRoleAsync(Guid roleId, AssignPermissionsToRoleReq req, CancellationToken cancellationToken = default);
}

public sealed class AssignPermissionsToRoleUseCase(IRoleRepository roleRepository) : IAssignPermissionsToRoleUseCase
{
    public async Task<RoleResponse> AssignPermissionsToRoleAsync(Guid roleId, AssignPermissionsToRoleReq req, CancellationToken cancellationToken = default)
    {
        var role = await roleRepository.GetRoleByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException("Role not found.");

        await roleRepository.SetRolePermissionsAsync(roleId, req.PermissionIds, cancellationToken);

        var updated = await roleRepository.GetRoleByIdWithPermissionsAsync(roleId, cancellationToken);
        var permissions = updated?.RolePermissions
            .Select(rp => new PermissionResponse(rp.PermissionId, rp.Permission.Name, rp.Permission.Description))
            .ToList() ?? [];

        return new RoleResponse(role.Id, role.Name, role.Description, permissions);
    }
}
