using System.ComponentModel.DataAnnotations;

namespace TCIP.Business.Modules.AccessControl.Application.Contracts;

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
