using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Projects;

public interface ICreateProjectUseCase
{
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, Guid ownerId, CancellationToken cancellationToken);
}

public sealed class CreateProjectUseCase(
    IProjectRepository projectRepository,
    TimeProvider timeProvider,
    IMembershipRepository membershipRepository,
    ICurrentPrincipalAccessor principalAccessor) : ICreateProjectUseCase
{
    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, Guid ownerId, CancellationToken cancellationToken)
    {
        if (!principalAccessor.IsAdmin() && !principalAccessor.HasGlobalPermission(Permissions.ProjectCreate) &&
            !await membershipRepository.IsAdminAsync(ownerId, cancellationToken) &&
            !await membershipRepository.HasPermissionAsync(ownerId, Permissions.ProjectCreate, Guid.Empty, cancellationToken))
            throw new ForbiddenException("Missing project.create permission.");

        await ValidateReferencesAsync(ownerId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var principalId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal { Id = principalId, Type = PrincipalType.Project },
            Name = Required(request.Name, "Project name"),
            Type = Required(request.Type, "Project type"),
            Description = Optional(request.Description),
            OwnerId = ownerId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await projectRepository.AddAsync(project, cancellationToken);
        membershipRepository.Add(new PrincipalMembership { UserId = ownerId, PrincipalId = principalId, IsOwner = true, JoinedAtUtc = now });
        await membershipRepository.SaveChangesAsync(cancellationToken);
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
