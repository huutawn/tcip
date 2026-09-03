using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Teams;

public interface IUpdateTeamUseCase
{
    Task<TeamResponse?> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken);
}

public sealed class UpdateTeamUseCase(
    ITeamRepository teamRepository,
    TimeProvider timeProvider) : IUpdateTeamUseCase
{
    public async Task<TeamResponse?> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetForUpdateAsync(id, cancellationToken);
        if (team is null) return null;
        var name = Required(request.Name, "Team name");
        if (await teamRepository.ExistsByNameAsync(name, id, cancellationToken))
            throw new ConflictException("A team with this name already exists.");
        team.Name = name;
        team.Description = Optional(request.Description);
        team.UpdatedAtUtc = timeProvider.GetUtcNow();
        await teamRepository.SaveChangesAsync(cancellationToken);
        return Map(team);
    }

    private static TeamResponse Map(Team team) => new(
        team.Id, team.PrincipalId, team.Name, team.Description, team.CreatedAtUtc, team.UpdatedAtUtc);

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new BadRequestException($"{field} is required.") : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
