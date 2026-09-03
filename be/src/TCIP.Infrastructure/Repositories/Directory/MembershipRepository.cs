using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Enums;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.Directory;

public sealed class MembershipRepository(TcipDbContext dbContext) : IMembershipRepository
{
    public Task<Guid?> GetPrincipalIdAsync(PrincipalType type, Guid resourceId, CancellationToken ct) => type switch
    {
        PrincipalType.Group => dbContext.Groups.Where(x => x.Id == resourceId).Select(x => (Guid?)x.PrincipalId).SingleOrDefaultAsync(ct),
        PrincipalType.Team => dbContext.Teams.Where(x => x.Id == resourceId).Select(x => (Guid?)x.PrincipalId).SingleOrDefaultAsync(ct),
        PrincipalType.Project => dbContext.Projects.Where(x => x.Id == resourceId).Select(x => (Guid?)x.PrincipalId).SingleOrDefaultAsync(ct),
        PrincipalType.Department => dbContext.Departments.Where(x => x.Id == resourceId).Select(x => (Guid?)x.PrincipalId).SingleOrDefaultAsync(ct),
        _ => Task.FromResult<Guid?>(null)
    };

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct) => dbContext.Users.AnyAsync(x => x.Id == userId, ct);

    public Task<PrincipalMembership?> GetAsync(Guid userId, Guid principalId, CancellationToken ct) =>
        dbContext.PrincipalMemberships.FindAsync([userId, principalId], ct).AsTask();

    public async Task<IReadOnlyList<(PrincipalMembership Membership, User User)>> GetActiveUsersAsync(Guid principalId, CancellationToken ct)
    {
        var rows = await dbContext.PrincipalMemberships.Include(x => x.User).Where(x => x.PrincipalId == principalId && x.LeftAtUtc == null).OrderBy(x => x.User.DisplayName).AsNoTracking().ToListAsync(ct);
        return rows.Select(x => (x, x.User)).ToArray();
    }

    public Task<bool> IsAdminAsync(Guid userId, CancellationToken ct) => dbContext.Users.AnyAsync(x => x.Id == userId && x.Role == UserRole.Admin, ct);

    public Task<Guid?> GetUserPrincipalIdAsync(Guid userId, CancellationToken ct) => dbContext.Users.Where(x => x.Id == userId).Select(x => (Guid?)x.PrincipalId).SingleOrDefaultAsync(ct);

    public async Task<HashSet<string>> GetPermissionsAsync(Guid userId, Guid resourcePrincipalId, CancellationToken ct)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return [];
        var roleIds = dbContext.RoleAssignments.Where(x => x.SubjectPrincipalId == user.PrincipalId && (x.ResourcePrincipalId == null || x.ResourcePrincipalId == resourcePrincipalId)).Select(x => x.RoleId);
        var names = dbContext.RolePermissions.Where(x => roleIds.Contains(x.RoleId)).Select(x => x.Permission.Name)
            .Concat(dbContext.PermissionGrants.Where(x => x.SubjectPrincipalId == user.PrincipalId && (x.ResourcePrincipalId == null || x.ResourcePrincipalId == resourcePrincipalId)).Select(x => x.Permission.Name));
        return (await names.Distinct().ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission, Guid resourcePrincipalId, CancellationToken ct) =>
        (await GetPermissionsAsync(userId, resourcePrincipalId, ct)).Contains(permission);

    public async Task<IReadOnlyList<string>> GetPermissionNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct) =>
        await dbContext.Permissions.Where(x => ids.Contains(x.Id)).Select(x => x.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetRolePermissionNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct) =>
        await dbContext.RolePermissions.Where(x => ids.Contains(x.RoleId)).Select(x => x.Permission.Name).Distinct().ToListAsync(ct);

    public async Task<(IReadOnlyList<Guid> RoleIds, IReadOnlyList<Guid> PermissionIds)> GetAccessAsync(Guid subjectPrincipalId, Guid resourcePrincipalId, CancellationToken ct) =>
        (await dbContext.RoleAssignments.Where(x => x.SubjectPrincipalId == subjectPrincipalId && x.ResourcePrincipalId == resourcePrincipalId).Select(x => x.RoleId).ToListAsync(ct),
         await dbContext.PermissionGrants.Where(x => x.SubjectPrincipalId == subjectPrincipalId && x.ResourcePrincipalId == resourcePrincipalId).Select(x => x.PermissionId).ToListAsync(ct));

    public async Task ReplaceAccessAsync(Guid userId, Guid resourcePrincipalId, IEnumerable<Guid> roleIds, IEnumerable<Guid> permissionIds, CancellationToken ct)
    {
        var principalId = await dbContext.Users.Where(x => x.Id == userId).Select(x => x.PrincipalId).SingleAsync(ct);
        dbContext.RoleAssignments.RemoveRange(await dbContext.RoleAssignments.Where(x => x.SubjectPrincipalId == principalId && x.ResourcePrincipalId == resourcePrincipalId).ToListAsync(ct));
        dbContext.PermissionGrants.RemoveRange(await dbContext.PermissionGrants.Where(x => x.SubjectPrincipalId == principalId && x.ResourcePrincipalId == resourcePrincipalId).ToListAsync(ct));
        await dbContext.RoleAssignments.AddRangeAsync(roleIds.Distinct().Select(roleId => new RoleAssignment { Id = Guid.NewGuid(), SubjectPrincipalId = principalId, RoleId = roleId, ResourcePrincipalId = resourcePrincipalId, CreatedAt = DateTimeOffset.UtcNow }), ct);
        await dbContext.PermissionGrants.AddRangeAsync(permissionIds.Distinct().Select(permissionId => new PermissionGrant { Id = Guid.NewGuid(), SubjectPrincipalId = principalId, PermissionId = permissionId, ResourcePrincipalId = resourcePrincipalId, CreatedAt = DateTimeOffset.UtcNow }), ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public Task<bool> HasAnotherOwnerAsync(Guid principalId, Guid excludedUserId, CancellationToken ct) =>
        dbContext.PrincipalMemberships.AnyAsync(x => x.PrincipalId == principalId && x.UserId != excludedUserId && x.LeftAtUtc == null && x.IsOwner, ct);

    public void Add(PrincipalMembership membership) => dbContext.PrincipalMemberships.Add(membership);

    public Task SaveChangesAsync(CancellationToken ct) => dbContext.SaveChangesAsync(ct);
}
