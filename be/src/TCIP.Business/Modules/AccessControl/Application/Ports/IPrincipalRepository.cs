using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;

namespace TCIP.Business.Modules.AccessControl.Application.Ports;

public interface IPrincipalRepository
{
    Task<Principal?> GetPrincipalByIdAsync(Guid id, CancellationToken ct = default);
    Task<Principal?> GetPrincipalDetailsByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Principal>> SearchPrincipalsAsync(PrincipalType? type, string? search, Guid? cursor, int limit, bool? available, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
