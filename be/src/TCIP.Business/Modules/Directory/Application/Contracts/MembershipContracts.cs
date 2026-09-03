namespace TCIP.Business.Modules.Directory.Application.Contracts;

public sealed record MemberResponse(
    Guid UserId,
    Guid PrincipalId,
    string Email,
    string DisplayName,
    bool IsOwner,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<Guid> PermissionIds);

public sealed record SetMemberRequest(
    bool IsMember,
    bool IsOwner,
    IReadOnlyList<Guid>? RoleIds = null,
    IReadOnlyList<Guid>? PermissionIds = null);
