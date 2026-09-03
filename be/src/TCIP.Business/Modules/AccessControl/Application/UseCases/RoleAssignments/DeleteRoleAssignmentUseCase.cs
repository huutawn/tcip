using TCIP.Business.Modules.AccessControl.Application.Ports;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.RoleAssignments;

public interface IDeleteRoleAssignmentUseCase
{
    Task<bool> DeleteRoleAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default);
}

public sealed class DeleteRoleAssignmentUseCase(IRoleAssignmentRepository roleAssignmentRepository) : IDeleteRoleAssignmentUseCase
{
    public async Task<bool> DeleteRoleAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await roleAssignmentRepository.GetRoleAssignmentByIdAsync(assignmentId, cancellationToken);
        if (assignment is null)
        {
            return false;
        }

        await roleAssignmentRepository.DeleteRoleAssignmentAsync(assignment, cancellationToken);
        return true;
    }
}
