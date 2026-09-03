using TCIP.Business.Modules.AccessControl.Domain.Models;

namespace TCIP.Business.Modules.AccessControl.Application.Ports;

public interface IAuthorizationSnapshotRepository
{
    Task<AuthorizationSnapshot> GetAuthorizationSnapshotAsync(Guid userId, CancellationToken ct = default);
}
