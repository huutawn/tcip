using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Enums;

namespace TCIP.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out userId);
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    private string permission = string.Empty;
    private string? resourceRoute;
    private PrincipalType resourceType;

    public PermissionAuthorizeAttribute(string permission)
    {
        Permission = permission;
        UpdatePolicy();
    }

    public string Permission
    {
        get => permission;
        set { permission = value; UpdatePolicy(); }
    }

    public string? ResourceRoute
    {
        get => resourceRoute;
        set { resourceRoute = value; UpdatePolicy(); }
    }

    public PrincipalType ResourceType
    {
        get => resourceType;
        set { resourceType = value; UpdatePolicy(); }
    }

    private void UpdatePolicy() => Policy = PermissionPolicyName.Build(permission, resourceRoute, resourceType);
}

public sealed record PermissionRequirement(
    string Permission,
    string? ResourceRoute,
    PrincipalType? ResourceType) : IAuthorizationRequirement;

public static class PermissionPolicyName
{
    private const string Prefix = "permission:";

    public static string Build(string permission, string? resourceRoute, PrincipalType? resourceType) =>
        Prefix + Uri.EscapeDataString(permission) + ":" + Uri.EscapeDataString(resourceRoute ?? string.Empty) + ":" + (resourceRoute is null ? string.Empty : resourceType.ToString());

    public static bool TryParse(string policyName, out PermissionRequirement requirement)
    {
        requirement = null!;
        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal)) return false;
        var parts = policyName[Prefix.Length..].Split(':', 3);
        if (parts.Length != 3) return false;
        if (!Enum.TryParse<PrincipalType>(parts[2], true, out var type)) type = default;
        var resourceRoute = Uri.UnescapeDataString(parts[1]);
        requirement = new PermissionRequirement(Uri.UnescapeDataString(parts[0]), string.IsNullOrEmpty(resourceRoute) ? null : resourceRoute, string.IsNullOrEmpty(parts[2]) ? null : type);
        return true;
    }
}

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider fallback = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!PermissionPolicyName.TryParse(policyName, out var requirement))
            return fallback.GetPolicyAsync(policyName);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(requirement)
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => fallback.GetFallbackPolicyAsync();
}

public sealed class PermissionAuthorizationHandler(
    IMembershipRepository membershipRepository)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (IsAdmin(context.User))
        {
            context.Succeed(requirement);
            return;
        }

        if (requirement.ResourceRoute is null)
        {
            if (HasClaim(context.User, PermissionClaimTypes.Permission, requirement.Permission))
                context.Succeed(requirement);
            return;
        }

        var httpContext = context.Resource switch
        {
            HttpContext value => value,
            AuthorizationFilterContext value => value.HttpContext,
            _ => null
        };
        if (httpContext is null || requirement.ResourceType is null)
            return;

        if (!Guid.TryParse(httpContext.Request.RouteValues[requirement.ResourceRoute]?.ToString(), out var resourceId))
            return;

        var resourcePrincipalId = await membershipRepository.GetPrincipalIdAsync(requirement.ResourceType.Value, resourceId, httpContext.RequestAborted);
        if (!resourcePrincipalId.HasValue)
            return;

        if (HasClaim(context.User, PermissionClaimTypes.ResourceOwner, resourcePrincipalId.Value.ToString("N")) ||
            HasClaim(context.User, PermissionClaimTypes.ResourcePermission, PermissionClaimTypes.ResourcePermissionValue(resourcePrincipalId.Value, requirement.Permission)))
        {
            context.Succeed(requirement);
        }
    }

    private static bool IsAdmin(ClaimsPrincipal principal) =>
        principal.IsInRole("Admin") || principal.Claims.Any(x => x.Type == PermissionClaimTypes.RbacAdmin && x.Value == "true");

    private static bool HasClaim(ClaimsPrincipal principal, string type, string value) =>
        principal.Claims.Any(claim => claim.Type == type && claim.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
}
