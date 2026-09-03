using TCIP.Business.Modules.Directory.Application.Ports;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Projects;

public interface IDeleteProjectUseCase
{
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class DeleteProjectUseCase(IProjectRepository projectRepository) : IDeleteProjectUseCase
{
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetForUpdateAsync(id, cancellationToken);
        if (project is null) return false;
        await projectRepository.DeleteAsync(project, cancellationToken);
        return true;
    }
}
