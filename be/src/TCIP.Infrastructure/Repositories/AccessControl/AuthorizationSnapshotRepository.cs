using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Identity.Domain.Enums;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.AccessControl;

public sealed class AuthorizationSnapshotRepository(TcipDbContext dbContext) : IAuthorizationSnapshotRepository
{
    public async Task<AuthorizationSnapshot> GetAuthorizationSnapshotAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found.");
        var isGlobalAdmin = user.Role == UserRole.Admin || await dbContext.RoleAssignments.AnyAsync(x => x.SubjectPrincipalId == user.PrincipalId && x.ResourcePrincipalId == null && x.Role.Name.ToLower() == BuiltInRbacCatalog.AdminRole.ToLower(), ct);
        if (isGlobalAdmin)
            return new AuthorizationSnapshot([], [], [], true);

        var globalRoleIds = dbContext.RoleAssignments
            .Where(x => x.SubjectPrincipalId == user.PrincipalId && x.ResourcePrincipalId == null)
            .Select(x => x.RoleId);
        var globalPermissions = await dbContext.RolePermissions
            .Where(x => globalRoleIds.Contains(x.RoleId))
            .Select(x => x.Permission.Name)
            .Concat(dbContext.PermissionGrants.Where(x => x.SubjectPrincipalId == user.PrincipalId && x.ResourcePrincipalId == null).Select(x => x.Permission.Name))
            .Distinct()
            .ToListAsync(ct);

        var memberships = await dbContext.PrincipalMemberships.AsNoTracking()
            .Where(x => x.UserId == userId && x.LeftAtUtc == null)
            .Select(x => new { x.PrincipalId, x.IsOwner })
            .ToListAsync(ct);
        var resourceIds = memberships.Select(x => x.PrincipalId).ToArray();
        if (resourceIds.Length == 0)
            return new AuthorizationSnapshot(globalPermissions, [], [], false);

        var roleAssignments = await dbContext.RoleAssignments
            .Where(x => x.SubjectPrincipalId == user.PrincipalId && x.ResourcePrincipalId.HasValue && resourceIds.Contains(x.ResourcePrincipalId.Value))
            .SelectMany(x => x.Role.RolePermissions.Select(rp => new { ResourcePrincipalId = x.ResourcePrincipalId!.Value, Permission = rp.Permission.Name }))
            .ToListAsync(ct);
        var directPermissions = await dbContext.PermissionGrants
            .Where(x => x.SubjectPrincipalId == user.PrincipalId && x.ResourcePrincipalId.HasValue && resourceIds.Contains(x.ResourcePrincipalId.Value))
            .Select(x => new { ResourcePrincipalId = x.ResourcePrincipalId!.Value, Permission = x.Permission.Name })
            .ToListAsync(ct);
        var resourcePermissions = roleAssignments.Concat(directPermissions)
            .GroupBy(x => x.ResourcePrincipalId)
            .Select(x => new ResourcePermissionSnapshot(x.Key, x.Select(y => y.Permission).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()))
            .ToArray();
        return new AuthorizationSnapshot(globalPermissions, resourcePermissions, memberships.Where(x => x.IsOwner).Select(x => x.PrincipalId).ToArray(), false);
    }
}
