using System.ComponentModel.DataAnnotations;

namespace TCIP.Business.Modules.AccessControl.Application.Contracts;

public sealed record PrincipalResponse(
    Guid PrincipalId,
    string Type,
    string Name,
    string? Description,
    string? Email,
    bool Available);

public sealed record PrincipalSearchQuery(
    string? Search = null,
    string? Type = null,
    bool? Available = null,
    string? Cursor = null,
    int Limit = 20);

public sealed record PrincipalSearchResponse(
    IReadOnlyList<PrincipalResponse> Items,
    string? NextCursor);

public sealed record SetPrincipalAvailabilityRequest(
    bool Available);

public sealed record PermissionResponse(
    Guid Id,
    string Name,
    string? Description);

public sealed record CreatePermissionReq(
    [Required, MaxLength(100)] string Name,
    [MaxLength(1000)] string? Description);

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<PermissionResponse> Permissions);

public sealed record CreateRoleReq(
    [Required, MaxLength(100)] string Name,
    [MaxLength(1000)] string? Description,
    IReadOnlyList<Guid>? PermissionIds = null);

public sealed record AssignPermissionsToRoleReq(
    IReadOnlyList<Guid> PermissionIds);

public sealed record RoleAssignmentResponse(
    Guid Id,
    Guid RoleId,
    Guid SubjectPrincipalId,
    Guid? ResourcePrincipalId,
    string? RoleName,
    DateTimeOffset CreatedAt);

public sealed record CreateRoleAssignmentReq(
    [Required] Guid RoleId,
    [Required] Guid SubjectPrincipalId,
    Guid? ResourcePrincipalId);

public sealed record PrincipalPermissionsResponse(
    Guid PrincipalId,
    Guid? ResourcePrincipalId,
    IReadOnlyList<string> Permissions);

public sealed record CheckPermissionResponse(
    Guid PrincipalId,
    string Permission,
    Guid? ResourcePrincipalId,
    bool Allowed);
