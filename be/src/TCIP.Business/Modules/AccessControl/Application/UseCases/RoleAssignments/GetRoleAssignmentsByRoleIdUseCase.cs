using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.RoleAssignments;

public interface IGetRoleAssignmentsByRoleIdUseCase
{
    Task<IEnumerable<RoleAssignmentResponse>> GetRoleAssignmentsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
}

public sealed class GetRoleAssignmentsByRoleIdUseCase(IRoleAssignmentRepository roleAssignmentRepository) : IGetRoleAssignmentsByRoleIdUseCase
{
    public async Task<IEnumerable<RoleAssignmentResponse>> GetRoleAssignmentsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var assignments = await roleAssignmentRepository.GetRoleAssignmentsByRoleIdAsync(roleId, cancellationToken);
        return assignments.Select(ra => new RoleAssignmentResponse(
            ra.Id,
            ra.RoleId,
            ra.SubjectPrincipalId,
            ra.ResourcePrincipalId,
            ra.Role?.Name,
            ra.CreatedAt
        ));
    }
}
