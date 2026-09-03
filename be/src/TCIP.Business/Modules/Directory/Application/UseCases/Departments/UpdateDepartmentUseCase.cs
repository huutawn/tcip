using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Departments;

public interface IUpdateDepartmentUseCase
{
    Task<DepartmentResponse?> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken);
}

public sealed class UpdateDepartmentUseCase(
    IDepartmentRepository departmentRepository,
    TimeProvider timeProvider) : IUpdateDepartmentUseCase
{
    public async Task<DepartmentResponse?> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var department = await departmentRepository.GetForUpdateAsync(id, cancellationToken);
        if (department is null) return null;
        var name = Required(request.Name, "Department name");
        if (await departmentRepository.ExistsByNameAsync(name, id, cancellationToken))
            throw new ConflictException("A department with this name already exists.");
        department.Name = name;
        department.Description = Optional(request.Description);
        department.UpdatedAtUtc = timeProvider.GetUtcNow();
        await departmentRepository.SaveChangesAsync(cancellationToken);
        return Map(department);
    }

    private static DepartmentResponse Map(Department department) => new(
        department.Id, department.PrincipalId, department.Name, department.Description, department.CreatedAtUtc, department.UpdatedAtUtc);

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new BadRequestException($"{field} is required.") : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
