using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Enums;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Memberships;

public interface IGetMembersUseCase
{
    Task<IReadOnlyList<MemberResponse>?> GetMembersAsync(PrincipalType type, Guid resourceId, CancellationToken ct);
}

public sealed class GetMembersUseCase(IMembershipRepository repository) : IGetMembersUseCase
{
    public async Task<IReadOnlyList<MemberResponse>?> GetMembersAsync(PrincipalType type, Guid resourceId, CancellationToken ct)
    {
        var principalId = await repository.GetPrincipalIdAsync(type, resourceId, ct);
        if (principalId is null) return null;
        var rows = await repository.GetActiveUsersAsync(principalId.Value, ct);
        var members = new List<MemberResponse>(rows.Count);
        foreach (var row in rows)
        {
            var access = await repository.GetAccessAsync(row.User.PrincipalId, principalId.Value, ct);
            members.Add(new MemberResponse(row.User.Id, row.User.PrincipalId, row.User.Email, row.User.DisplayName, row.Membership.IsOwner, access.RoleIds, access.PermissionIds));
        }
        return members;
    }
}
