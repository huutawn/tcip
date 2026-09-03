using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Teams;

public interface IGetTeamByIdUseCase
{
    Task<TeamResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class GetTeamByIdUseCase(ITeamRepository teamRepository) : IGetTeamByIdUseCase
{
    public async Task<TeamResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await teamRepository.GetByIdAsync(id, cancellationToken)) is { } team ? Map(team) : null;

    private static TeamResponse Map(Team team) => new(
        team.Id, team.PrincipalId, team.Name, team.Description, team.CreatedAtUtc, team.UpdatedAtUtc);
}
