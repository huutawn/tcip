using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Projects;

public interface IUpdateProjectUseCase
{
    Task<ProjectResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken);
}

public sealed class UpdateProjectUseCase(
    IProjectRepository projectRepository,
    TimeProvider timeProvider) : IUpdateProjectUseCase
{
    public async Task<ProjectResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetForUpdateAsync(id, cancellationToken);
        if (project is null) return null;
        await ValidateReferencesAsync(request.OwnerId, cancellationToken);

        project.Name = Required(request.Name, "Project name");
        project.Type = Required(request.Type, "Project type");
        project.Description = Optional(request.Description);
        project.OwnerId = request.OwnerId;
        project.UpdatedAtUtc = timeProvider.GetUtcNow();
        await projectRepository.SaveChangesAsync(cancellationToken);
        return Map(project);
    }

    private async Task ValidateReferencesAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        if (!await projectRepository.OwnerExistsAsync(ownerId, cancellationToken))
            throw new NotFoundException("Project owner not found.");
    }

    private static ProjectResponse Map(Project project) => new(
        project.Id, project.PrincipalId, project.Name, project.Type, project.Description,
        project.OwnerId, project.CreatedAtUtc, project.UpdatedAtUtc);

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new BadRequestException($"{field} is required.") : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
