using TCIP.Business.Modules.AccessControl.Application.Ports;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.Roles;

public interface IDeleteRoleUseCase
{
    Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}

public sealed class DeleteRoleUseCase(IRoleRepository roleRepository) : IDeleteRoleUseCase
{
    public async Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await roleRepository.GetRoleByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            return false;
        }

        await roleRepository.DeleteRoleAsync(role, cancellationToken);
        return true;
    }
}
