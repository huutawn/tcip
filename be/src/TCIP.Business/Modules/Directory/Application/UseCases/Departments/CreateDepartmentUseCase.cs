using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Departments;

public interface ICreateDepartmentUseCase
{
    Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request, Guid actorUserId, CancellationToken cancellationToken);
}

public sealed class CreateDepartmentUseCase(
    IDepartmentRepository departmentRepository,
    IMembershipRepository membershipRepository,
    TimeProvider timeProvider,
    ICurrentPrincipalAccessor principalAccessor) : ICreateDepartmentUseCase
{
    public async Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        var name = Required(request.Name, "Department name");
        if (await departmentRepository.ExistsByNameAsync(name, null, cancellationToken))
            throw new ConflictException("A department with this name already exists.");

        if (!principalAccessor.IsAdmin() && !principalAccessor.HasGlobalPermission(Permissions.DepartmentCreate) &&
            !await membershipRepository.IsAdminAsync(actorUserId, cancellationToken) &&
            !await membershipRepository.HasPermissionAsync(actorUserId, Permissions.DepartmentCreate, Guid.Empty, cancellationToken))
            throw new ForbiddenException("Missing department.create permission.");

        var now = timeProvider.GetUtcNow();
        var principalId = Guid.NewGuid();
        var department = new Department
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal { Id = principalId, Type = PrincipalType.Department },
            Name = name,
            Description = Optional(request.Description),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var membership = new PrincipalMembership { UserId = actorUserId, PrincipalId = principalId, IsOwner = true, JoinedAtUtc = now };
        await departmentRepository.CreateDepartmentAsync(department, membership, cancellationToken);
        return Map(department);
    }

    private static DepartmentResponse Map(Department department) => new(
        department.Id, department.PrincipalId, department.Name, department.Description, department.CreatedAtUtc, department.UpdatedAtUtc);

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new BadRequestException($"{field} is required.") : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
