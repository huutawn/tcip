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
