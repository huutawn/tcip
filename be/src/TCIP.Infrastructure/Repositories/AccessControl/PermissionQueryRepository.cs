using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.Identity.Domain.Enums;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.AccessControl;

public sealed class PermissionQueryRepository(TcipDbContext dbContext) : IPermissionQueryRepository
{
    public async Task<IEnumerable<string>> GetPermissionsForPrincipalAsync(Guid principalId, Guid? resourceId = null, CancellationToken ct = default)
    {
        var principal = await dbContext.Principals.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == principalId && x.Available, ct);
        if (principal is null) return [];
        var subjectId = principal.User?.PrincipalId ?? principalId;
        if (principal.User?.Role == UserRole.Admin) return await dbContext.Permissions.Select(x => x.Name).ToListAsync(ct);
        if (resourceId.HasValue)
        {
            var membership = await dbContext.PrincipalMemberships.FirstOrDefaultAsync(x => x.UserId == principal.User!.Id && x.PrincipalId == resourceId.Value && x.LeftAtUtc == null, ct);
            if (membership?.IsOwner == true) return await dbContext.Permissions.Select(x => x.Name).ToListAsync(ct);
            if (membership is null)
            {
                resourceId = null;
            }
        }
        var roleIds = dbContext.RoleAssignments.Where(x => x.SubjectPrincipalId == subjectId && (x.ResourcePrincipalId == null || x.ResourcePrincipalId == resourceId)).Select(x => x.RoleId);
        var roles = dbContext.RolePermissions.Where(x => roleIds.Contains(x.RoleId)).Select(x => x.Permission.Name);
        var direct = dbContext.PermissionGrants.Where(x => x.SubjectPrincipalId == subjectId && (x.ResourcePrincipalId == null || x.ResourcePrincipalId == resourceId)).Select(x => x.Permission.Name);
        return await roles.Concat(direct).Distinct().ToListAsync(ct);
    }

    public async Task<bool> HasPermissionAsync(Guid principalId, string permissionName, Guid? resourceId = null, CancellationToken ct = default) =>
        (await GetPermissionsForPrincipalAsync(principalId, resourceId, ct)).Any(x => x.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
}
