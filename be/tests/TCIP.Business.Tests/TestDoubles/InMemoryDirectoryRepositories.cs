using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Business.Modules.Identity.Domain.Entities;

namespace TCIP.Business.Tests.TestDoubles;

public sealed class InMemoryDepartmentRepository : IDepartmentRepository
{
    public readonly Dictionary<Guid, Department> Departments = new();

    public Task<bool> ExistsByNameAsync(string name, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Departments.Values.Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && (!exceptId.HasValue || d.Id != exceptId.Value)));

    public Task CreateDepartmentAsync(Department department, PrincipalMembership membership, CancellationToken cancellationToken)
    {
        Departments[department.Id] = department;
        return Task.CompletedTask;
    }

    public Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Departments.GetValueOrDefault(id));

    public Task<Department?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Departments.GetValueOrDefault(id));

    public Task DeleteAsync(Department department, CancellationToken cancellationToken)
    {
        Departments.Remove(department.Id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class InMemoryTeamRepository : ITeamRepository
{
    public readonly Dictionary<Guid, Team> Teams = new();

    public Task<bool> ExistsByNameAsync(string name, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Teams.Values.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && (!exceptId.HasValue || t.Id != exceptId.Value)));

    public Task AddAsync(Team team, CancellationToken cancellationToken)
    {
        Teams[team.Id] = team;
        return Task.CompletedTask;
    }

    public Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Teams.GetValueOrDefault(id));

    public Task<Team?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Teams.GetValueOrDefault(id));

    public Task DeleteAsync(Team team, CancellationToken cancellationToken)
    {
        Teams.Remove(team.Id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class InMemoryProjectRepository : IProjectRepository
{
    public readonly Dictionary<Guid, Project> Projects = new();
    public readonly HashSet<Guid> ExistingOwners = new();

    public Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        Projects[project.Id] = project;
        return Task.CompletedTask;
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Projects.GetValueOrDefault(id));

    public Task<Project?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Projects.GetValueOrDefault(id));

    public Task<bool> OwnerExistsAsync(Guid ownerId, CancellationToken cancellationToken) =>
        Task.FromResult(ExistingOwners.Contains(ownerId));

    public Task DeleteAsync(Project project, CancellationToken cancellationToken)
    {
        Projects.Remove(project.Id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class InMemoryGroupRepository : IGroupRepository
{
    public readonly Dictionary<Guid, Group> Groups = new();

    public Task<bool> ExistsByNameAndTypeAsync(string name, string type, CancellationToken cancellationToken) =>
        Task.FromResult(Groups.Values.Any(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && g.Type.Equals(type, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(Group group, CancellationToken cancellationToken)
    {
        Groups[group.Id] = group;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryMembershipRepository : IMembershipRepository
{
    public readonly Dictionary<(Guid UserId, Guid PrincipalId), PrincipalMembership> Memberships = new();
    public readonly Dictionary<Guid, Guid> ResourceToPrincipal = new();
    public readonly HashSet<Guid> ExistingUsers = new();
    public readonly HashSet<Guid> Admins = new();

    public Task<Guid?> GetPrincipalIdAsync(PrincipalType type, Guid resourceId, CancellationToken ct) =>
        Task.FromResult(ResourceToPrincipal.TryGetValue(resourceId, out var p) ? (Guid?)p : null);

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult(ExistingUsers.Contains(userId));

    public Task<PrincipalMembership?> GetAsync(Guid userId, Guid principalId, CancellationToken ct) =>
        Task.FromResult(Memberships.GetValueOrDefault((userId, principalId)));

    public Task<IReadOnlyList<(PrincipalMembership Membership, User User)>> GetActiveUsersAsync(Guid principalId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<(PrincipalMembership, User)>>([]);

    public Task<bool> IsAdminAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult(Admins.Contains(userId));

    public Task<Guid?> GetUserPrincipalIdAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<Guid?>(userId);

    public Task<HashSet<string>> GetPermissionsAsync(Guid userId, Guid resourcePrincipalId, CancellationToken ct) =>
        Task.FromResult(new HashSet<string>());

    public Task<bool> HasPermissionAsync(Guid userId, string permission, Guid resourcePrincipalId, CancellationToken ct) =>
        Task.FromResult(true);

    public Task<IReadOnlyList<string>> GetPermissionNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetRolePermissionNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<(IReadOnlyList<Guid> RoleIds, IReadOnlyList<Guid> PermissionIds)> GetAccessAsync(Guid subjectPrincipalId, Guid resourcePrincipalId, CancellationToken ct) =>
        Task.FromResult<(IReadOnlyList<Guid>, IReadOnlyList<Guid>)>(([], []));

    public Task ReplaceAccessAsync(Guid userId, Guid resourcePrincipalId, IEnumerable<Guid> roleIds, IEnumerable<Guid> permissionIds, CancellationToken ct) =>
        Task.CompletedTask;

    public Task<bool> HasAnotherOwnerAsync(Guid principalId, Guid excludedUserId, CancellationToken ct) =>
        Task.FromResult(Memberships.Values.Any(m => m.PrincipalId == principalId && m.UserId != excludedUserId && m.IsOwner && m.LeftAtUtc == null));

    public void Add(PrincipalMembership membership) =>
        Memberships[(membership.UserId, membership.PrincipalId)] = membership;

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

public sealed class TestPrincipalAccessor : ICurrentPrincipalAccessor
{
    public Guid? CurrentUserId { get; set; }
    public bool AdminFlag { get; set; } = true;
    public HashSet<string> GlobalPermissions { get; } = new();

    public Guid? GetCurrentUserId() => CurrentUserId;
    public bool IsAdmin() => AdminFlag;
    public bool HasGlobalPermission(string permission) => GlobalPermissions.Contains(permission);
    public bool IsResourceOwner(Guid resourcePrincipalId) => true;
    public bool HasResourcePermission(Guid resourcePrincipalId, string permission) => true;
    public HashSet<string> GetResourcePermissions(Guid resourcePrincipalId) => [];
}
