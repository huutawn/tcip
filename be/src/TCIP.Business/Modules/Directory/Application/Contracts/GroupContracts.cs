using System.ComponentModel.DataAnnotations;

namespace TCIP.Business.Modules.Directory.Application.Contracts;

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
