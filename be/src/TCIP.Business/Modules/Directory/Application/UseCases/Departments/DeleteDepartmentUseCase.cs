using TCIP.Business.Modules.Directory.Application.Ports;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Departments;

public interface IDeleteDepartmentUseCase
{
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class DeleteDepartmentUseCase(IDepartmentRepository departmentRepository) : IDeleteDepartmentUseCase
{
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var department = await departmentRepository.GetForUpdateAsync(id, cancellationToken);
        if (department is null) return false;
        await departmentRepository.DeleteAsync(department, cancellationToken);
        return true;
    }
}
