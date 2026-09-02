using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Business.Modules.Identity.Domain.Entities;

namespace TCIP.Business.Modules.Directory.Application.Ports;

public interface IDepartmentRepository
{
    Task<bool> ExistsByNameAsync(string name, Guid? exceptId, CancellationToken cancellationToken);
    Task CreateDepartmentAsync(Department department, PrincipalMembership membership, CancellationToken cancellationToken);
    Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Department?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteAsync(Department department, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IGroupRepository
{
    Task<bool> ExistsByNameAndTypeAsync(string name, string type, CancellationToken cancellationToken);
    Task AddAsync(Group group, CancellationToken cancellationToken);
}

public interface ITeamRepository
{
    Task<bool> ExistsByNameAsync(string name, Guid? exceptId, CancellationToken cancellationToken);
    Task AddAsync(Team team, CancellationToken cancellationToken);
    Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Team?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteAsync(Team team, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Project?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> OwnerExistsAsync(Guid ownerId, CancellationToken cancellationToken);
    Task DeleteAsync(Project project, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IMembershipRepository
{
    Task<Guid?> GetPrincipalIdAsync(PrincipalType type, Guid resourceId, CancellationToken ct);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken ct);
    Task<PrincipalMembership?> GetAsync(Guid userId, Guid principalId, CancellationToken ct);
    Task<IReadOnlyList<(PrincipalMembership Membership, User User)>> GetActiveUsersAsync(Guid principalId, CancellationToken ct);
    Task<bool> IsAdminAsync(Guid userId, CancellationToken ct);
    Task<Guid?> GetUserPrincipalIdAsync(Guid userId, CancellationToken ct);
    Task<HashSet<string>> GetPermissionsAsync(Guid userId, Guid resourcePrincipalId, CancellationToken ct);
    Task<bool> HasPermissionAsync(Guid userId, string permission, Guid resourcePrincipalId, CancellationToken ct);
    Task<IReadOnlyList<string>> GetPermissionNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<IReadOnlyList<string>> GetRolePermissionNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<(IReadOnlyList<Guid> RoleIds, IReadOnlyList<Guid> PermissionIds)> GetAccessAsync(Guid subjectPrincipalId, Guid resourcePrincipalId, CancellationToken ct);
    Task ReplaceAccessAsync(Guid userId, Guid resourcePrincipalId, IEnumerable<Guid> roleIds, IEnumerable<Guid> permissionIds, CancellationToken ct);
    Task<bool> HasAnotherOwnerAsync(Guid principalId, Guid excludedUserId, CancellationToken ct);
    void Add(PrincipalMembership membership);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IDirectoryRecipientResolver
{
    Task<IReadOnlyList<Guid>> GetRecipientsForAudiencesAsync(
        IReadOnlyCollection<Guid> audiencePrincipalIds,
        DateTimeOffset resolvedAtUtc,
        Guid? cursor,
        int limit,
        CancellationToken cancellationToken);
}
