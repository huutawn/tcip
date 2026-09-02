using System.ComponentModel.DataAnnotations;

namespace TCIP.Business.Modules.Directory.Application.Contracts;

public sealed record DepartmentResponse(
    Guid Id,
    Guid PrincipalId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateDepartmentRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(1000)] string? Description);

public sealed record UpdateDepartmentRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(1000)] string? Description);

public sealed record GroupResponse(
    Guid Id,
    Guid PrincipalId,
    string Name,
    string? Description,
    string Type);

public sealed record CreateGroupReq(
    [Required, MaxLength(100)] string Name,
    [MaxLength(1000)] string? Description,
    [Required, MaxLength(64)] string Type);

public sealed record TeamResponse(
    Guid Id,
    Guid PrincipalId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateTeamRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(1000)] string? Description);

public sealed record UpdateTeamRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(1000)] string? Description);

public sealed record ProjectResponse(
    Guid Id,
    Guid PrincipalId,
    string Name,
    string Type,
    string? Description,
    Guid OwnerId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateProjectRequest(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(64)] string Type,
    [MaxLength(1000)] string? Description);

public sealed record UpdateProjectRequest(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(64)] string Type,
    [MaxLength(1000)] string? Description,
    [Required] Guid OwnerId);

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
