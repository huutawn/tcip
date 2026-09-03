using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.RoleAssignments;

public interface ICreateRoleAssignmentUseCase
{
    Task<RoleAssignmentResponse> CreateRoleAssignmentAsync(CreateRoleAssignmentReq req, CancellationToken cancellationToken = default);
}

public sealed class CreateRoleAssignmentUseCase(
    IRoleAssignmentRepository roleAssignmentRepository,
    IRoleRepository roleRepository,
    IPrincipalRepository principalRepository,
    TimeProvider timeProvider) : ICreateRoleAssignmentUseCase
{
    public async Task<RoleAssignmentResponse> CreateRoleAssignmentAsync(CreateRoleAssignmentReq req, CancellationToken cancellationToken = default)
    {
        var role = await roleRepository.GetRoleByIdAsync(req.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role not found.");

        var principal = await principalRepository.GetPrincipalByIdAsync(req.SubjectPrincipalId, cancellationToken)
            ?? throw new NotFoundException("Principal not found.");
        if (!principal.Available)
        {
            throw new BadRequestException("Unavailable principals cannot be assigned a role.");
        }

        var existing = await roleAssignmentRepository.GetRoleAssignmentAsync(req.SubjectPrincipalId, req.RoleId, req.ResourcePrincipalId, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Role is already assigned to this principal in the specified scope.");
        }

        var assignment = new RoleAssignment
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            SubjectPrincipalId = principal.Id,
            ResourcePrincipalId = req.ResourcePrincipalId,
            CreatedAt = timeProvider.GetUtcNow()
        };

        await roleAssignmentRepository.CreateRoleAssignmentAsync(assignment, cancellationToken);

        return new RoleAssignmentResponse(
            assignment.Id,
            assignment.RoleId,
            assignment.SubjectPrincipalId,
            assignment.ResourcePrincipalId,
            role.Name,
            assignment.CreatedAt
        );
    }
}
