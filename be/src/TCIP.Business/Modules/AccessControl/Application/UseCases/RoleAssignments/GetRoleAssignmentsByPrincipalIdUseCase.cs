using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.RoleAssignments;

public interface IGetRoleAssignmentsByPrincipalIdUseCase
{
    Task<IEnumerable<RoleAssignmentResponse>> GetRoleAssignmentsByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default);
}

public sealed class GetRoleAssignmentsByPrincipalIdUseCase(IRoleAssignmentRepository roleAssignmentRepository) : IGetRoleAssignmentsByPrincipalIdUseCase
{
    public async Task<IEnumerable<RoleAssignmentResponse>> GetRoleAssignmentsByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default)
    {
        var assignments = await roleAssignmentRepository.GetRoleAssignmentsByPrincipalIdAsync(principalId, cancellationToken);
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
