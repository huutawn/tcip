using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Directory.Application.UseCases;

public interface IDepartmentUseCase
{
    Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request, Guid actorUserId, CancellationToken cancellationToken);
    Task<DepartmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DepartmentResponse?> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class DepartmentUseCase(
    IDepartmentRepository departmentRepository,
    TimeProvider timeProvider,
    IMembershipRepository membershipRepository,
    ICurrentPrincipalAccessor principalAccessor) : IDepartmentUseCase
{
    public async Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        var name = Required(request.Name, "Department name");
        if (await departmentRepository.ExistsByNameAsync(name, null, cancellationToken))
            throw new ConflictException("A department with this name already exists.");

        if (!principalAccessor.IsAdmin() && !principalAccessor.HasGlobalPermission(Permissions.DepartmentCreate) &&
            !await membershipRepository.IsAdminAsync(actorUserId, cancellationToken) &&
            !await membershipRepository.HasPermissionAsync(actorUserId, Permissions.DepartmentCreate, Guid.Empty, cancellationToken))
            throw new ForbiddenException("Missing department.create permission.");

        var now = timeProvider.GetUtcNow();
        var principalId = Guid.NewGuid();
        var department = new Department
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal { Id = principalId, Type = PrincipalType.Department },
            Name = name,
            Description = Optional(request.Description),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var membership = new PrincipalMembership { UserId = actorUserId, PrincipalId = principalId, IsOwner = true, JoinedAtUtc = now };
        await departmentRepository.CreateDepartmentAsync(department, membership, cancellationToken);
        return Map(department);
    }

    public async Task<DepartmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await departmentRepository.GetByIdAsync(id, cancellationToken)) is { } department ? Map(department) : null;

    public async Task<DepartmentResponse?> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var department = await departmentRepository.GetForUpdateAsync(id, cancellationToken);
        if (department is null) return null;
        var name = Required(request.Name, "Department name");
        if (await departmentRepository.ExistsByNameAsync(name, id, cancellationToken))
            throw new ConflictException("A department with this name already exists.");
        department.Name = name;
        department.Description = Optional(request.Description);
        department.UpdatedAtUtc = timeProvider.GetUtcNow();
        await departmentRepository.SaveChangesAsync(cancellationToken);
        return Map(department);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var department = await departmentRepository.GetForUpdateAsync(id, cancellationToken);
        if (department is null) return false;
        await departmentRepository.DeleteAsync(department, cancellationToken);
        return true;
    }

    private static DepartmentResponse Map(Department department) => new(
        department.Id, department.PrincipalId, department.Name, department.Description, department.CreatedAtUtc, department.UpdatedAtUtc);

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new BadRequestException($"{field} is required.") : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public interface IGroupUseCase
{
    Task<GroupResponse> CreateAsync(CreateGroupReq request, Guid actorUserId, CancellationToken cancellationToken);
}

public sealed class GroupUseCase(
    IGroupRepository groupRepository,
    IMembershipRepository membershipRepository,
    TimeProvider timeProvider,
    ICurrentPrincipalAccessor principalAccessor) : IGroupUseCase
{
    public async Task<GroupResponse> CreateAsync(
        CreateGroupReq request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var type = request.Type.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
        {
            throw new BadRequestException("Group name and type are required.");
        }

        if (await groupRepository.ExistsByNameAndTypeAsync(name, type, cancellationToken))
        {
            throw new ConflictException("A group with this name and type already exists.");
        }

        if (!principalAccessor.IsAdmin() && !principalAccessor.HasGlobalPermission(Permissions.GroupCreate) &&
            !await membershipRepository.IsAdminAsync(actorUserId, cancellationToken) &&
            !await membershipRepository.HasPermissionAsync(actorUserId, Permissions.GroupCreate, Guid.Empty, cancellationToken))
            throw new ForbiddenException("Missing group.create permission.");

        var principalId = Guid.NewGuid();
        var group = new Group
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal
            {
                Id = principalId,
                Type = PrincipalType.Group
            },
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            Type = type
        };
        await groupRepository.AddAsync(group, cancellationToken);
        membershipRepository.Add(new PrincipalMembership { UserId = actorUserId, PrincipalId = principalId, IsOwner = true, JoinedAtUtc = timeProvider.GetUtcNow() });
        await membershipRepository.SaveChangesAsync(cancellationToken);
        return new GroupResponse(group.Id, group.PrincipalId, group.Name, group.Description, group.Type);
    }
}

public interface ITeamUseCase
{
    Task<TeamResponse> CreateAsync(CreateTeamRequest request, Guid actorUserId, CancellationToken cancellationToken);
    Task<TeamResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TeamResponse?> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class TeamUseCase(
    ITeamRepository teamRepository,
    TimeProvider timeProvider,
    IMembershipRepository membershipRepository,
    ICurrentPrincipalAccessor principalAccessor) : ITeamUseCase
{
    public async Task<TeamResponse> CreateAsync(CreateTeamRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        var name = Required(request.Name, "Team name");
        if (await teamRepository.ExistsByNameAsync(name, null, cancellationToken))
            throw new ConflictException("A team with this name already exists.");

        if (!principalAccessor.IsAdmin() && !principalAccessor.HasGlobalPermission(Permissions.TeamCreate) &&
            !await membershipRepository.IsAdminAsync(actorUserId, cancellationToken) &&
            !await membershipRepository.HasPermissionAsync(actorUserId, Permissions.TeamCreate, Guid.Empty, cancellationToken))
            throw new ForbiddenException("Missing team.create permission.");

        var now = timeProvider.GetUtcNow();
        var principalId = Guid.NewGuid();
        var team = new Team
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal { Id = principalId, Type = PrincipalType.Team },
            Name = name,
            Description = Optional(request.Description),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await teamRepository.AddAsync(team, cancellationToken);
        membershipRepository.Add(new PrincipalMembership { UserId = actorUserId, PrincipalId = principalId, IsOwner = true, JoinedAtUtc = now });
        await membershipRepository.SaveChangesAsync(cancellationToken);
        return Map(team);
    }

    public async Task<TeamResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await teamRepository.GetByIdAsync(id, cancellationToken)) is { } team ? Map(team) : null;

    public async Task<TeamResponse?> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetForUpdateAsync(id, cancellationToken);
        if (team is null) return null;
        var name = Required(request.Name, "Team name");
        if (await teamRepository.ExistsByNameAsync(name, id, cancellationToken))
            throw new ConflictException("A team with this name already exists.");
        team.Name = name;
        team.Description = Optional(request.Description);
        team.UpdatedAtUtc = timeProvider.GetUtcNow();
        await teamRepository.SaveChangesAsync(cancellationToken);
        return Map(team);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetForUpdateAsync(id, cancellationToken);
        if (team is null) return false;
        await teamRepository.DeleteAsync(team, cancellationToken);
        return true;
    }

    private static TeamResponse Map(Team team) => new(
        team.Id, team.PrincipalId, team.Name, team.Description, team.CreatedAtUtc, team.UpdatedAtUtc);

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new BadRequestException($"{field} is required.") : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public interface IProjectUseCase
{
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, Guid ownerId, CancellationToken cancellationToken);
    Task<ProjectResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ProjectResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class ProjectUseCase(
    IProjectRepository projectRepository,
    TimeProvider timeProvider,
    IMembershipRepository membershipRepository,
    ICurrentPrincipalAccessor principalAccessor) : IProjectUseCase
{
    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, Guid ownerId, CancellationToken cancellationToken)
    {
        if (!principalAccessor.IsAdmin() && !principalAccessor.HasGlobalPermission(Permissions.ProjectCreate) &&
            !await membershipRepository.IsAdminAsync(ownerId, cancellationToken) &&
            !await membershipRepository.HasPermissionAsync(ownerId, Permissions.ProjectCreate, Guid.Empty, cancellationToken))
            throw new ForbiddenException("Missing project.create permission.");

        await ValidateReferencesAsync(ownerId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var principalId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal { Id = principalId, Type = PrincipalType.Project },
            Name = Required(request.Name, "Project name"),
            Type = Required(request.Type, "Project type"),
            Description = Optional(request.Description),
            OwnerId = ownerId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await projectRepository.AddAsync(project, cancellationToken);
        membershipRepository.Add(new PrincipalMembership { UserId = ownerId, PrincipalId = principalId, IsOwner = true, JoinedAtUtc = now });
        await membershipRepository.SaveChangesAsync(cancellationToken);
        return Map(project);
    }

    public async Task<ProjectResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await projectRepository.GetByIdAsync(id, cancellationToken)) is { } project ? Map(project) : null;

    public async Task<ProjectResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetForUpdateAsync(id, cancellationToken);
        if (project is null) return null;
        await ValidateReferencesAsync(request.OwnerId, cancellationToken);

        project.Name = Required(request.Name, "Project name");
        project.Type = Required(request.Type, "Project type");
        project.Description = Optional(request.Description);
        project.OwnerId = request.OwnerId;
        project.UpdatedAtUtc = timeProvider.GetUtcNow();
        await projectRepository.SaveChangesAsync(cancellationToken);
        return Map(project);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetForUpdateAsync(id, cancellationToken);
        if (project is null) return false;
        await projectRepository.DeleteAsync(project, cancellationToken);
        return true;
    }

    private async Task ValidateReferencesAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        if (!await projectRepository.OwnerExistsAsync(ownerId, cancellationToken))
            throw new NotFoundException("Project owner not found.");
    }

    private static ProjectResponse Map(Project project) => new(
        project.Id, project.PrincipalId, project.Name, project.Type, project.Description,
        project.OwnerId, project.CreatedAtUtc, project.UpdatedAtUtc);

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new BadRequestException($"{field} is required.") : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public interface IMembershipUseCase
{
    Task<bool> SetMemberAsync(PrincipalType type, Guid resourceId, Guid actorUserId, Guid userId, SetMemberRequest request, CancellationToken ct);
    Task<IReadOnlyList<MemberResponse>?> GetMembersAsync(PrincipalType type, Guid resourceId, CancellationToken ct);
}

public sealed class MembershipUseCase(
    IMembershipRepository repository,
    TimeProvider timeProvider,
    ICurrentPrincipalAccessor principalAccessor) : IMembershipUseCase
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

    public async Task<IReadOnlyList<MemberResponse>?> GetMembersAsync(PrincipalType type, Guid resourceId, CancellationToken ct)
    {
        var principalId = await repository.GetPrincipalIdAsync(type, resourceId, ct);
        if (principalId is null) return null;
        var rows = await repository.GetActiveUsersAsync(principalId.Value, ct);
        var members = new List<MemberResponse>(rows.Count);
        foreach (var row in rows)
        {
            var access = await repository.GetAccessAsync(row.User.PrincipalId, principalId.Value, ct);
            members.Add(new MemberResponse(row.User.Id, row.User.PrincipalId, row.User.Email, row.User.DisplayName, row.Membership.IsOwner, access.RoleIds, access.PermissionIds));
        }
        return members;
    }

    private async Task<HashSet<string>> GetPermissionNamesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        return (await repository.GetPermissionNamesByIdsAsync(ids, ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
