using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.Principals;

public interface IGetPrincipalByIdUseCase
{
    Task<PrincipalResponse?> GetPrincipalByIdAsync(Guid principalId, CancellationToken cancellationToken = default);
}

public sealed class GetPrincipalByIdUseCase(IPrincipalRepository principalRepository) : IGetPrincipalByIdUseCase
{
    public async Task<PrincipalResponse?> GetPrincipalByIdAsync(Guid principalId, CancellationToken cancellationToken = default)
    {
        var principal = await principalRepository.GetPrincipalDetailsByIdAsync(principalId, cancellationToken);
        return principal is null ? throw new NotFoundException("Principal not found") : MapPrincipal(principal);
    }

    private static PrincipalResponse MapPrincipal(Principal principal) => principal.Type switch
    {
        PrincipalType.User when principal.User is not null => new(
            principal.Id, principal.Type.ToString(), principal.User.DisplayName, null,
            principal.User.Email, principal.Available),
        PrincipalType.Group when principal.Group is not null => new(
            principal.Id, principal.Type.ToString(), principal.Group.Name,
            principal.Group.Description, null, principal.Available),
        PrincipalType.Team when principal.Team is not null => new(
            principal.Id, principal.Type.ToString(), principal.Team.Name,
            principal.Team.Description, null, principal.Available),
        PrincipalType.Project when principal.Project is not null => new(
            principal.Id, principal.Type.ToString(), principal.Project.Name,
            principal.Project.Description, null, principal.Available),
        PrincipalType.Department when principal.Department is not null => new(
            principal.Id, principal.Type.ToString(), principal.Department.Name,
            principal.Department.Description, null, principal.Available),
        _ => new(principal.Id, principal.Type.ToString(), string.Empty, null, null, principal.Available)
    };
}
