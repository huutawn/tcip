using System.ComponentModel.DataAnnotations;
using TCIP.Business.Modules.Identity.Domain.Enums;

namespace TCIP.Business.Modules.Identity.Application.Contracts;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required, MaxLength(100)] string DisplayName);

public sealed record RegisterResponse(
    Guid Id,
    Guid PrincipalId,
    string Email,
    string DisplayName,
    DateTimeOffset CreatedAtUtc);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);

public sealed record RefreshTokenRequest(
    [Required] string RefreshToken);

public sealed record UserResponse(
    Guid Id,
    Guid PrincipalId,
    string Email,
    string DisplayName,
    bool EmailVerified,
    string Language,
    string TimeZoneId,
    UserRole Role,
    DateTimeOffset CreatedAtUtc);

public sealed record PagedUsersResponse(
    IReadOnlyList<UserResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record UserListQuery(
    int Page = 1,
    int PageSize = 50);

public sealed record UpdateUserRoleRequest(
    UserRole Role);
