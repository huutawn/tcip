using TCIP.Business.Modules.AccessControl.Domain.Entities;

namespace TCIP.Business.Modules.AccessControl.Application.Ports;

public interface IRoleAssignmentRepository
{
    Task<RoleAssignment> CreateRoleAssignmentAsync(RoleAssignment item, CancellationToken ct = default);
    Task<RoleAssignment?> GetRoleAssignmentByIdAsync(Guid id, CancellationToken ct = default);
    Task<RoleAssignment?> GetRoleAssignmentAsync(Guid subjectId, Guid roleId, Guid? resourceId, CancellationToken ct = default);
    Task<IEnumerable<Role>> GetRolesByPrincipalIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByPrincipalIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByRoleIdAsync(Guid id, CancellationToken ct = default);
    Task DeleteRoleAssignmentAsync(RoleAssignment item, CancellationToken ct = default);
    Task<bool> DeleteRoleAssignmentsAsync(Guid subjectId, Guid? resourceId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
