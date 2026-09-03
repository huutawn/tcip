using System.ComponentModel.DataAnnotations;

namespace TCIP.Business.Modules.Directory.Application.Contracts;

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
