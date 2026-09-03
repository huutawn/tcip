using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Enums;

namespace TCIP.Infrastructure.Adapters.Security;

public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> hasher = new();

    public string HashPassword(User user, string password) =>
        hasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
    {
        var result = hasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }
}

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToHexString(bytes);
    }

    public string Hash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}

public sealed class IdentityConfigurationAdapter(IConfiguration configuration) : IIdentityConfiguration
{
    public int AccessTokenMinutes => configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60;
    public int RefreshTokenDays => configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 7;
}

public sealed class JwtTokenIssuer(
    IConfiguration configuration,
    IAuthorizationSnapshotRepository authorizationSnapshotRepository) : ITokenIssuer
{
    public async Task<string> GenerateAccessTokenAsync(
        User user,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT issuer missing.");

        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT audience missing.");

        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key missing.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("display_name", user.DisplayName),
            new("role", user.Role.ToString()),
            new("principal_id", user.PrincipalId.ToString())
        };

        var snapshot = await authorizationSnapshotRepository.GetAuthorizationSnapshotAsync(user.Id, cancellationToken);
        claims.AddRange(snapshot.GlobalPermissions.Select(permission => new Claim(PermissionClaimTypes.Permission, permission)));
        if (snapshot.IsGlobalAdmin) claims.Add(new Claim(PermissionClaimTypes.RbacAdmin, "true"));
        claims.AddRange(snapshot.OwnedResourcePrincipalIds.Select(id => new Claim(PermissionClaimTypes.ResourceOwner, id.ToString("N"))));
        claims.AddRange(snapshot.ResourcePermissions.SelectMany(resource => resource.Permissions.Select(permission => new Claim(
            PermissionClaimTypes.ResourcePermission,
            PermissionClaimTypes.ResourcePermissionValue(resource.ResourcePrincipalId, permission)))));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class CurrentPrincipalAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentPrincipalAccessor
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? GetCurrentUserId()
    {
        var sub = Principal?.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public bool IsAdmin()
    {
        var p = Principal;
        if (p is null) return false;
        return p.IsInRole(UserRole.Admin.ToString()) || p.Claims.Any(x => x.Type == PermissionClaimTypes.RbacAdmin && x.Value == "true");
    }

    public bool HasGlobalPermission(string permission)
    {
        var p = Principal;
        if (p is null) return false;
        return p.Claims.Any(x => x.Type == PermissionClaimTypes.Permission && x.Value.Equals(permission, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsResourceOwner(Guid resourcePrincipalId)
    {
        var p = Principal;
        if (p is null) return false;
        return p.Claims.Any(x => x.Type == PermissionClaimTypes.ResourceOwner && x.Value.Equals(resourcePrincipalId.ToString("N"), StringComparison.OrdinalIgnoreCase));
    }

    public bool HasResourcePermission(Guid resourcePrincipalId, string permission)
    {
        var p = Principal;
        if (p is null) return false;
        return IsResourceOwner(resourcePrincipalId) ||
               p.Claims.Any(x => x.Type == PermissionClaimTypes.ResourcePermission && x.Value.Equals(PermissionClaimTypes.ResourcePermissionValue(resourcePrincipalId, permission), StringComparison.OrdinalIgnoreCase));
    }

    public HashSet<string> GetResourcePermissions(Guid resourcePrincipalId)
    {
        var p = Principal;
        if (p is null) return [];
        var permissions = p.Claims.Where(x => x.Type == PermissionClaimTypes.Permission).Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var prefix = resourcePrincipalId.ToString("N") + "|";
        foreach (var value in p.Claims.Where(x => x.Type == PermissionClaimTypes.ResourcePermission).Select(x => x.Value))
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) permissions.Add(value[prefix.Length..]);
        }
        return permissions;
    }
}
