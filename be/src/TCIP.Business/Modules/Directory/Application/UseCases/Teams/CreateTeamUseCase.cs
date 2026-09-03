using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Teams;

public interface ICreateTeamUseCase
{
    Task<TeamResponse> CreateAsync(CreateTeamRequest request, Guid actorUserId, CancellationToken cancellationToken);
}

public sealed class CreateTeamUseCase(
    ITeamRepository teamRepository,
    TimeProvider timeProvider,
    IMembershipRepository membershipRepository,
    ICurrentPrincipalAccessor principalAccessor) : ICreateTeamUseCase
{
    public async Task<TeamResponse> CreateAsync(CreateTeamRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        var name = Required(request.Name, "Team name");
        if (await teamRepository.ExistsByNameAsync(name, null, cancellationToken))
            throw new ConflictException("A team with this name already exists.");

        if (!principalAccessor.IsAdmin() && !principalAccessor.HasGlobalPermission(Permissions.TeamCreate) &&
            !await membershipRepository.IsAdminAsync(actorUserId, cancellationToken) &&
            !await membershipRepository.HasPermissionAsync(actorUserId, Permissions.TeamCreate, Guid.Empty, cancellationToken))
            throw new ForbiddenException("Missing team.create permission.");

        var now = timeProvider.GetUtcNow();
        var principalId = Guid.NewGuid();
        var team = new Team
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal { Id = principalId, Type = PrincipalType.Team },
            Name = name,
            Description = Optional(request.Description),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await teamRepository.AddAsync(team, cancellationToken);
        membershipRepository.Add(new PrincipalMembership { UserId = actorUserId, PrincipalId = principalId, IsOwner = true, JoinedAtUtc = now });
        await membershipRepository.SaveChangesAsync(cancellationToken);
        return Map(team);
    }

    private static TeamResponse Map(Team team) => new(
        team.Id, team.PrincipalId, team.Name, team.Description, team.CreatedAtUtc, team.UpdatedAtUtc);

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new BadRequestException($"{field} is required.") : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
