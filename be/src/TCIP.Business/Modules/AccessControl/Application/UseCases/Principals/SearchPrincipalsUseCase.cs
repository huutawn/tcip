using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.Principals;

public interface ISearchPrincipalsUseCase
{
    Task<PrincipalSearchResponse> SearchPrincipalsAsync(PrincipalSearchQuery query, CancellationToken cancellationToken = default);
}

public sealed class SearchPrincipalsUseCase(IPrincipalRepository principalRepository) : ISearchPrincipalsUseCase
{
    public async Task<PrincipalSearchResponse> SearchPrincipalsAsync(
        PrincipalSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Limit is < 1 or > 100)
            throw new BadRequestException("Limit must be between 1 and 100.");

        PrincipalType? type = null;
        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            if (!Enum.TryParse<PrincipalType>(query.Type, true, out var parsedType))
            {
                throw new BadRequestException($"Invalid principal type: '{query.Type}'.");
            }

            type = parsedType;
        }

        Guid? cursor = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!Guid.TryParse(query.Cursor, out var parsedCursor))
                throw new BadRequestException("Cursor must be a principal ID.");
            cursor = parsedCursor;
        }

        var principals = await principalRepository.SearchPrincipalsAsync(
            type, query.Search, cursor, query.Limit, query.Available, cancellationToken);
        var hasNextPage = principals.Count > query.Limit;
        var items = principals.Take(query.Limit).Select(MapPrincipal).ToArray();
        return new PrincipalSearchResponse(
            items,
            hasNextPage ? items[^1].PrincipalId.ToString("N") : null);
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
