using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Memberships;

public interface ISetMemberUseCase
{
    Task<bool> SetMemberAsync(PrincipalType type, Guid resourceId, Guid actorUserId, Guid userId, SetMemberRequest request, CancellationToken ct);
}

public sealed class SetMemberUseCase(
    IMembershipRepository repository,
    TimeProvider timeProvider,
    ICurrentPrincipalAccessor principalAccessor) : ISetMemberUseCase
{
    public async Task<bool> SetMemberAsync(PrincipalType type, Guid resourceId, Guid actorUserId, Guid userId, SetMemberRequest request, CancellationToken ct)
    {
        var principalId = await repository.GetPrincipalIdAsync(type, resourceId, ct);
        if (principalId is null || !await repository.UserExistsAsync(userId, ct)) return false;
        var actorMembership = await repository.GetAsync(actorUserId, principalId.Value, ct);

        var admin = principalAccessor.IsAdmin() || await repository.IsAdminAsync(actorUserId, ct);
        var actorIsOwner = actorMembership?.IsOwner == true || principalAccessor.IsResourceOwner(principalId.Value);
        var canManage = admin || actorIsOwner || principalAccessor.HasResourcePermission(principalId.Value, Permissions.MembershipManage) || await repository.HasPermissionAsync(actorUserId, Permissions.MembershipManage, principalId.Value, ct);
        var canManageOwners = admin || actorIsOwner || principalAccessor.HasResourcePermission(principalId.Value, Permissions.OwnerManage) || await repository.HasPermissionAsync(actorUserId, Permissions.OwnerManage, principalId.Value, ct);

        if (!canManage) throw new ForbiddenException("You cannot manage membership for this resource.");
        if (request.IsOwner && !canManageOwners) throw new ForbiddenException("Only an owner or admin can assign owner.");

        var requestedPermissions = request.PermissionIds ?? [];
        var assigningAccess = (request.RoleIds?.Count ?? 0) > 0 || requestedPermissions.Count > 0;
        if (!admin && !actorIsOwner)
        {
            var hasAccessGrant = principalAccessor.HasResourcePermission(principalId.Value, Permissions.AccessGrant) || await repository.HasPermissionAsync(actorUserId, Permissions.AccessGrant, principalId.Value, ct);
            if (assigningAccess && !hasAccessGrant)
                throw new ForbiddenException("Missing access.grant permission.");
            var effective = principalAccessor.GetResourcePermissions(principalId.Value);
            if (effective.Count == 0)
            {
                effective = await repository.GetPermissionsAsync(actorUserId, principalId.Value, ct);
            }
            var rolePermissions = await repository.GetRolePermissionNamesByIdsAsync(request.RoleIds ?? [], ct);
            if (requestedPermissions.Count > 0 || rolePermissions.Count > 0)
            {
                var names = await GetPermissionNamesAsync(requestedPermissions, ct);
                names.UnionWith(rolePermissions);
                if (names.Any(name => !effective.Contains(name))) throw new ForbiddenException("You cannot grant permissions you do not have.");
            }
        }
        var membership = await repository.GetAsync(userId, principalId.Value, ct);
        if (!request.IsMember)
        {
            if (membership is null) return true;
            if (membership.IsOwner && !canManageOwners) throw new ForbiddenException("Only an owner or admin can remove an owner.");
            if (membership.IsOwner && !await repository.HasAnotherOwnerAsync(principalId.Value, userId, ct)) throw new ConflictException("A resource must retain at least one owner.");
            membership.LeftAtUtc = timeProvider.GetUtcNow(); membership.IsOwner = false;
            await repository.SaveChangesAsync(ct); return true;
        }
        if (membership is not null && membership.IsOwner && !request.IsOwner && !canManageOwners)
            throw new ForbiddenException("Only an owner or admin can demote an owner.");
        if (membership is not null && membership.IsOwner && !request.IsOwner && !await repository.HasAnotherOwnerAsync(principalId.Value, userId, ct))
            throw new ConflictException("A resource must retain at least one owner.");
        if (membership is null)
        {
            repository.Add(new PrincipalMembership { UserId = userId, PrincipalId = principalId.Value, IsOwner = request.IsOwner, JoinedAtUtc = timeProvider.GetUtcNow() });
        }
        else { membership.LeftAtUtc = null; membership.JoinedAtUtc = timeProvider.GetUtcNow(); membership.IsOwner = request.IsOwner; await repository.SaveChangesAsync(ct); }
        await repository.ReplaceAccessAsync(userId, principalId.Value, request.RoleIds ?? [], requestedPermissions, ct);
        return true;
    }

    private async Task<HashSet<string>> GetPermissionNamesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        return (await repository.GetPermissionNamesByIdsAsync(ids, ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
