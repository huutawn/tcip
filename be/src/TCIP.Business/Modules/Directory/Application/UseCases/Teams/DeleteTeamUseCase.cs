using TCIP.Business.Modules.Directory.Application.Ports;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Teams;

public interface IDeleteTeamUseCase
{
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class DeleteTeamUseCase(ITeamRepository teamRepository) : IDeleteTeamUseCase
{
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetForUpdateAsync(id, cancellationToken);
        if (team is null) return false;
        await teamRepository.DeleteAsync(team, cancellationToken);
        return true;
    }
}
