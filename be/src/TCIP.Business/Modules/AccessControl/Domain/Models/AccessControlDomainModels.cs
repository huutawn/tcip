namespace TCIP.Business.Modules.AccessControl.Domain.Models;

public static class Permissions
{
    public const string UserRead = "user.read";
    public const string UserManage = "user.manage";
    public const string RoleRead = "role.read";
    public const string RoleManage = "role.manage";
    public const string PermissionRead = "permission.read";
    public const string PermissionManage = "permission.manage";
    public const string PrincipalRead = "principal.read";
    public const string PrincipalManage = "principal.manage";
    public const string GroupCreate = "group.create";
    public const string GroupRead = "group.read";
    public const string GroupUpdate = "group.update";
    public const string GroupDelete = "group.delete";
    public const string TeamCreate = "team.create";
    public const string TeamRead = "team.read";
    public const string TeamUpdate = "team.update";
    public const string TeamDelete = "team.delete";
    public const string ProjectCreate = "project.create";
    public const string ProjectRead = "project.read";
    public const string ProjectUpdate = "project.update";
    public const string ProjectDelete = "project.delete";
    public const string DepartmentCreate = "department.create";
    public const string DepartmentRead = "department.read";
    public const string DepartmentUpdate = "department.update";
    public const string DepartmentDelete = "department.delete";
    public const string MembershipRead = "membership.read";
    public const string MembershipManage = "membership.manage";
    public const string AccessGrant = "access.grant";
    public const string OwnerManage = "owner.manage";
}

public static class BuiltInRbacCatalog
{
    public const string AdminRole = "Admin";
    public const string CreatorRole = "Creator";
    public const string ManagerRole = "Manager";
    public const string EditorRole = "Editor";
    public const string ViewerRole = "Viewer";

    public static IReadOnlyList<(string Name, string? Description)> AllPermissions =>
    [
        (Permissions.UserRead, "View users."),
        (Permissions.UserManage, "Manage users and their roles."),
        (Permissions.RoleRead, "View roles."),
        (Permissions.RoleManage, "Manage roles and role permissions."),
        (Permissions.PermissionRead, "View permissions."),
        (Permissions.PermissionManage, "Manage permissions."),
        (Permissions.PrincipalRead, "View principals."),
        (Permissions.PrincipalManage, "Manage principal availability."),
        (Permissions.GroupCreate, "Create groups."),
        (Permissions.GroupRead, "View groups."),
        (Permissions.GroupUpdate, "Update groups."),
        (Permissions.GroupDelete, "Delete groups."),
        (Permissions.TeamCreate, "Create teams."),
        (Permissions.TeamRead, "View teams."),
        (Permissions.TeamUpdate, "Update teams."),
        (Permissions.TeamDelete, "Delete teams."),
        (Permissions.ProjectCreate, "Create projects."),
        (Permissions.ProjectRead, "View projects."),
        (Permissions.ProjectUpdate, "Update projects."),
        (Permissions.ProjectDelete, "Delete projects."),
        (Permissions.DepartmentCreate, "Create departments."),
        (Permissions.DepartmentRead, "View departments."),
        (Permissions.DepartmentUpdate, "Update departments."),
        (Permissions.DepartmentDelete, "Delete departments."),
        (Permissions.MembershipRead, "View resource members."),
        (Permissions.MembershipManage, "Add, update, and remove resource members."),
        (Permissions.AccessGrant, "Grant roles and direct permissions."),
        (Permissions.OwnerManage, "Promote or demote resource owners.")
    ];

    public static IReadOnlyList<(string Name, string? Description, IReadOnlyList<string> Permissions)> Roles =>
    [
        (AdminRole, "Full RBAC catalog and resource access.", AllPermissions.Select(x => x.Name).ToArray()),
        (CreatorRole, "Create groups, teams, projects, and departments.", [Permissions.GroupCreate, Permissions.TeamCreate, Permissions.ProjectCreate, Permissions.DepartmentCreate]),
        (ManagerRole, "Manage members and resource access.", [Permissions.MembershipRead, Permissions.MembershipManage, Permissions.AccessGrant]),
        (EditorRole, "Read and update resources.", [Permissions.GroupRead, Permissions.GroupUpdate, Permissions.TeamRead, Permissions.TeamUpdate, Permissions.ProjectRead, Permissions.ProjectUpdate, Permissions.DepartmentRead, Permissions.DepartmentUpdate]),
        (ViewerRole, "Read resources and members.", [Permissions.GroupRead, Permissions.TeamRead, Permissions.ProjectRead, Permissions.DepartmentRead, Permissions.MembershipRead])
    ];
}

public static class PermissionClaimTypes
{
    public const string Permission = "permission";
    public const string ResourcePermission = "resource_permission";
    public const string ResourceOwner = "resource_owner";
    public const string RbacAdmin = "rbac_admin";

    public static string ResourcePermissionValue(Guid resourcePrincipalId, string permission) =>
        $"{resourcePrincipalId:N}|{permission}";
}

public sealed record ResourcePermissionSnapshot(
    Guid ResourcePrincipalId,
    IReadOnlyList<string> Permissions);

public sealed record AuthorizationSnapshot(
    IReadOnlyList<string> GlobalPermissions,
    IReadOnlyList<ResourcePermissionSnapshot> ResourcePermissions,
    IReadOnlyList<Guid> OwnedResourcePrincipalIds,
    bool IsGlobalAdmin);
