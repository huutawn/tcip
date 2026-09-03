using System.ComponentModel.DataAnnotations;

namespace TCIP.Business.Modules.Directory.Application.Contracts;

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
