using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Projects;

public interface IGetProjectByIdUseCase
{
    Task<ProjectResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class GetProjectByIdUseCase(IProjectRepository projectRepository) : IGetProjectByIdUseCase
{
    public async Task<ProjectResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await projectRepository.GetByIdAsync(id, cancellationToken)) is { } project ? Map(project) : null;

    private static ProjectResponse Map(Project project) => new(
        project.Id, project.PrincipalId, project.Name, project.Type, project.Description,
        project.OwnerId, project.CreatedAtUtc, project.UpdatedAtUtc);
}
