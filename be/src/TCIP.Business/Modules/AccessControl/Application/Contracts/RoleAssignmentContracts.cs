using System.ComponentModel.DataAnnotations;

namespace TCIP.Business.Modules.AccessControl.Application.Contracts;

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
