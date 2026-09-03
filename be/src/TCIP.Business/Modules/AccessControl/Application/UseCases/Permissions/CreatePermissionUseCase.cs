using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.Permissions;

public interface ICreatePermissionUseCase
{
    Task<PermissionResponse> CreatePermissionAsync(CreatePermissionReq req, CancellationToken cancellationToken = default);
}

public sealed class CreatePermissionUseCase(IPermissionRepository permissionRepository) : ICreatePermissionUseCase
{
    public async Task<PermissionResponse> CreatePermissionAsync(CreatePermissionReq req, CancellationToken cancellationToken = default)
    {
        var name = req.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Permission name is required.");
        }

        if (await permissionRepository.PermissionExistsByNameAsync(name, cancellationToken))
        {
            throw new ConflictException($"Permission '{name}' already exists.");
        }

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim()
        };

        await permissionRepository.CreatePermissionAsync(permission, cancellationToken);
        return new PermissionResponse(permission.Id, permission.Name, permission.Description);
    }
}
