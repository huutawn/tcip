using TCIP.Business.Modules.AccessControl.Application.Contracts;
using TCIP.Business.Modules.AccessControl.Application.UseCases.PermissionQueries;
using TCIP.Business.Modules.AccessControl.Application.UseCases.Permissions;
using TCIP.Business.Modules.AccessControl.Application.UseCases.Principals;
using TCIP.Business.Modules.AccessControl.Application.UseCases.RoleAssignments;
using TCIP.Business.Modules.AccessControl.Application.UseCases.Roles;
using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Business.Tests.TestDoubles;
using TCIP.Common.Exceptions;
using Xunit;

namespace TCIP.Business.Tests;

public sealed class AccessControlUseCasesTests
{
    [Fact]
    public async Task CreatePermission_ValidationAndConflict_Enforced()
    {
        var repo = new InMemoryPermissionRepository();
        var useCase = new CreatePermissionUseCase(repo);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            useCase.CreatePermissionAsync(new CreatePermissionReq("", null)));

        var created = await useCase.CreatePermissionAsync(new CreatePermissionReq("event.read", "Read events"));
        Assert.Equal("event.read", created.Name);

        await Assert.ThrowsAsync<ConflictException>(() =>
            useCase.CreatePermissionAsync(new CreatePermissionReq("event.read", "Duplicate")));
    }

    [Fact]
    public async Task GetAndDeletePermission_NotFoundAndSuccess()
    {
        var repo = new InMemoryPermissionRepository();
        var getUseCase = new GetPermissionByIdUseCase(repo);
        var deleteUseCase = new DeletePermissionUseCase(repo);

        var id = Guid.NewGuid();
        Assert.Null(await getUseCase.GetPermissionByIdAsync(id));
        Assert.False(await deleteUseCase.DeletePermissionAsync(id));

        var perm = new Permission { Id = id, Name = "user.read" };
        await repo.CreatePermissionAsync(perm);

        var found = await getUseCase.GetPermissionByIdAsync(id);
        Assert.NotNull(found);
        Assert.Equal("user.read", found.Name);

        Assert.True(await deleteUseCase.DeletePermissionAsync(id));
        Assert.Null(await getUseCase.GetPermissionByIdAsync(id));
    }

    [Fact]
    public async Task CreateRole_DuplicateAndPermissions_Handled()
    {
        var roleRepo = new InMemoryRoleRepository();
        var permRepo = new InMemoryPermissionRepository();
        var perm = await permRepo.CreatePermissionAsync(new Permission { Id = Guid.NewGuid(), Name = "role.read" });

        var useCase = new CreateRoleUseCase(roleRepo, permRepo);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            useCase.CreateRoleAsync(new CreateRoleReq(" ", null)));

        var role = await useCase.CreateRoleAsync(new CreateRoleReq("Manager", "Manager role", [perm.Id]));
        Assert.Equal("Manager", role.Name);
        Assert.Single(role.Permissions);

        await Assert.ThrowsAsync<ConflictException>(() =>
            useCase.CreateRoleAsync(new CreateRoleReq("Manager", null)));
    }

    [Fact]
    public async Task AssignPermissionsToRole_RoleNotFound_Throws()
    {
        var roleRepo = new InMemoryRoleRepository();
        var useCase = new AssignPermissionsToRoleUseCase(roleRepo);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.AssignPermissionsToRoleAsync(Guid.NewGuid(), new AssignPermissionsToRoleReq([Guid.NewGuid()])));
    }

    [Fact]
    public async Task RoleAssignment_UnavailablePrincipal_ThrowsBadRequest()
    {
        var assignRepo = new InMemoryRoleAssignmentRepository();
        var roleRepo = new InMemoryRoleRepository();
        var principalRepo = new InMemoryPrincipalRepository();

        var role = await roleRepo.CreateRoleAsync(new Role { Id = Guid.NewGuid(), Name = "Viewer" });
        var unavailablePrincipal = new Principal { Id = Guid.NewGuid(), Type = PrincipalType.User, Available = false };
        principalRepo.Principals[unavailablePrincipal.Id] = unavailablePrincipal;

        var useCase = new CreateRoleAssignmentUseCase(assignRepo, roleRepo, principalRepo, TimeProvider.System);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            useCase.CreateRoleAssignmentAsync(new CreateRoleAssignmentReq(role.Id, unavailablePrincipal.Id, null)));
    }

    [Fact]
    public async Task RoleAssignment_DuplicateScope_ThrowsConflict()
    {
        var assignRepo = new InMemoryRoleAssignmentRepository();
        var roleRepo = new InMemoryRoleRepository();
        var principalRepo = new InMemoryPrincipalRepository();

        var role = await roleRepo.CreateRoleAsync(new Role { Id = Guid.NewGuid(), Name = "Viewer" });
        var principal = new Principal { Id = Guid.NewGuid(), Type = PrincipalType.User, Available = true };
        principalRepo.Principals[principal.Id] = principal;

        var useCase = new CreateRoleAssignmentUseCase(assignRepo, roleRepo, principalRepo, TimeProvider.System);

        var created = await useCase.CreateRoleAssignmentAsync(new CreateRoleAssignmentReq(role.Id, principal.Id, null));
        Assert.NotNull(created);

        await Assert.ThrowsAsync<ConflictException>(() =>
            useCase.CreateRoleAssignmentAsync(new CreateRoleAssignmentReq(role.Id, principal.Id, null)));
    }

    [Fact]
    public async Task Principals_Validation_LimitsAndCursor()
    {
        var repo = new InMemoryPrincipalRepository();
        var searchUseCase = new SearchPrincipalsUseCase(repo);
        var getUseCase = new GetPrincipalByIdUseCase(repo);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            searchUseCase.SearchPrincipalsAsync(new PrincipalSearchQuery(Limit: 0)));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            searchUseCase.SearchPrincipalsAsync(new PrincipalSearchQuery(Limit: 101)));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            searchUseCase.SearchPrincipalsAsync(new PrincipalSearchQuery(Type: "InvalidType")));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            searchUseCase.SearchPrincipalsAsync(new PrincipalSearchQuery(Cursor: "not-a-guid")));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            getUseCase.GetPrincipalByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task PermissionQuery_CheckAndGet_ReturnsCorrectMapping()
    {
        var queryRepo = new InMemoryPermissionQueryRepository();
        var principalId = Guid.NewGuid();
        queryRepo.PrincipalPermissions[principalId] = ["event.read", "event.write"];

        var checkUseCase = new CheckPermissionUseCase(queryRepo);
        var getUseCase = new GetPermissionsForPrincipalUseCase(queryRepo);

        var checkTrue = await checkUseCase.CheckPermissionAsync(principalId, "event.read", null);
        Assert.True(checkTrue.Allowed);

        var checkFalse = await checkUseCase.CheckPermissionAsync(principalId, "admin.all", null);
        Assert.False(checkFalse.Allowed);

        var perms = await getUseCase.GetPermissionsForPrincipalAsync(principalId, null);
        Assert.Equal(2, perms.Permissions.Count);
        Assert.Contains("event.read", perms.Permissions);
    }
}
