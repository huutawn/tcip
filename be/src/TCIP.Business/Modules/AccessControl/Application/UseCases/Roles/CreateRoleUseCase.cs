using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.Roles;

public interface ICreateRoleUseCase
{
    Task<RoleResponse> CreateRoleAsync(CreateRoleReq req, CancellationToken cancellationToken = default);
}

public sealed class CreateRoleUseCase(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository) : ICreateRoleUseCase
{
    public async Task<RoleResponse> CreateRoleAsync(CreateRoleReq req, CancellationToken cancellationToken = default)
    {
        var name = req.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Role name is required.");
        }

        if (await roleRepository.RoleExistsByNameAsync(name, cancellationToken))
        {
            throw new ConflictException($"Role '{name}' already exists.");
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim()
        };

        if (req.PermissionIds is { Count: > 0 })
        {
            var validPermissions = await permissionRepository.GetPermissionsByIdsAsync(req.PermissionIds, cancellationToken);
            foreach (var p in validPermissions)
            {
                role.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = p.Id,
                    Permission = p
                });
            }
        }

        await roleRepository.CreateRoleAsync(role, cancellationToken);

        var permissions = role.RolePermissions
            .Select(rp => new PermissionResponse(rp.PermissionId, rp.Permission?.Name ?? "", rp.Permission?.Description))
            .ToList();

        return new RoleResponse(role.Id, role.Name, role.Description, permissions);
    }
}
