using System.ComponentModel.DataAnnotations;

namespace TCIP.Business.Modules.AccessControl.Application.Contracts;

public sealed record PermissionResponse(
    Guid Id,
    string Name,
    string? Description);

public sealed record CreatePermissionReq(
    [Required, MaxLength(100)] string Name,
    [MaxLength(1000)] string? Description);
