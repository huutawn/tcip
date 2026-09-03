using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Departments;

public interface IGetDepartmentByIdUseCase
{
    Task<DepartmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class GetDepartmentByIdUseCase(IDepartmentRepository departmentRepository) : IGetDepartmentByIdUseCase
{
    public async Task<DepartmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await departmentRepository.GetByIdAsync(id, cancellationToken)) is { } department ? Map(department) : null;

    private static DepartmentResponse Map(Department department) => new(
        department.Id, department.PrincipalId, department.Name, department.Description, department.CreatedAtUtc, department.UpdatedAtUtc);
}
