using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.UseCases.Departments;
using TCIP.Business.Modules.Directory.Application.UseCases.Groups;
using TCIP.Business.Modules.Directory.Application.UseCases.Memberships;
using TCIP.Business.Modules.Directory.Application.UseCases.Projects;
using TCIP.Business.Modules.Directory.Application.UseCases.Teams;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Business.Tests.TestDoubles;
using TCIP.Common.Exceptions;
using Xunit;

namespace TCIP.Business.Tests;

public sealed class DirectoryUseCasesTests
{
    [Fact]
    public async Task DepartmentUseCases_CrudFlow_Validated()
    {
        var deptRepo = new InMemoryDepartmentRepository();
        var memRepo = new InMemoryMembershipRepository();
        var principalAccessor = new TestPrincipalAccessor();
        var timeProvider = TimeProvider.System;
        var actorId = Guid.NewGuid();

        var create = new CreateDepartmentUseCase(deptRepo, memRepo, timeProvider, principalAccessor);
        var get = new GetDepartmentByIdUseCase(deptRepo);
        var update = new UpdateDepartmentUseCase(deptRepo, timeProvider);
        var delete = new DeleteDepartmentUseCase(deptRepo);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            create.CreateAsync(new CreateDepartmentRequest("", null), actorId, default));

        var created = await create.CreateAsync(new CreateDepartmentRequest("Engineering", "Tech dept"), actorId, default);
        Assert.Equal("Engineering", created.Name);

        await Assert.ThrowsAsync<ConflictException>(() =>
            create.CreateAsync(new CreateDepartmentRequest("Engineering", null), actorId, default));

        var fetched = await get.GetByIdAsync(created.Id, default);
        Assert.NotNull(fetched);

        var updated = await update.UpdateAsync(created.Id, new UpdateDepartmentRequest("Product & Eng", null), default);
        Assert.NotNull(updated);
        Assert.Equal("Product & Eng", updated.Name);

        Assert.True(await delete.DeleteAsync(created.Id, default));
        Assert.Null(await get.GetByIdAsync(created.Id, default));
    }

    [Fact]
    public async Task TeamUseCases_CrudFlow_Validated()
    {
        var teamRepo = new InMemoryTeamRepository();
        var memRepo = new InMemoryMembershipRepository();
        var principalAccessor = new TestPrincipalAccessor();
        var timeProvider = TimeProvider.System;
        var actorId = Guid.NewGuid();

        var create = new CreateTeamUseCase(teamRepo, timeProvider, memRepo, principalAccessor);
        var get = new GetTeamByIdUseCase(teamRepo);
        var update = new UpdateTeamUseCase(teamRepo, timeProvider);
        var delete = new DeleteTeamUseCase(teamRepo);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            create.CreateAsync(new CreateTeamRequest(" ", null), actorId, default));

        var team = await create.CreateAsync(new CreateTeamRequest("Backend", null), actorId, default);
        Assert.Equal("Backend", team.Name);

        await Assert.ThrowsAsync<ConflictException>(() =>
            create.CreateAsync(new CreateTeamRequest("Backend", null), actorId, default));

        var fetched = await get.GetByIdAsync(team.Id, default);
        Assert.NotNull(fetched);

        Assert.True(await delete.DeleteAsync(team.Id, default));
        Assert.Null(await get.GetByIdAsync(team.Id, default));
    }

    [Fact]
    public async Task ProjectUseCases_OwnerValidation_Enforced()
    {
        var projectRepo = new InMemoryProjectRepository();
        var memRepo = new InMemoryMembershipRepository();
        var principalAccessor = new TestPrincipalAccessor();
        var timeProvider = TimeProvider.System;
        var ownerId = Guid.NewGuid();

        var create = new CreateProjectUseCase(projectRepo, timeProvider, memRepo, principalAccessor);

        // Owner does not exist
        await Assert.ThrowsAsync<NotFoundException>(() =>
            create.CreateAsync(new CreateProjectRequest("Project Alpha", "Software", null), ownerId, default));

        // Add owner
        projectRepo.ExistingOwners.Add(ownerId);
        var project = await create.CreateAsync(new CreateProjectRequest("Project Alpha", "Software", null), ownerId, default);
        Assert.Equal("Project Alpha", project.Name);

        var get = new GetProjectByIdUseCase(projectRepo);
        var fetched = await get.GetByIdAsync(project.Id, default);
        Assert.NotNull(fetched);
    }

    [Fact]
    public async Task GroupUseCase_ValidationAndConflict()
    {
        var groupRepo = new InMemoryGroupRepository();
        var memRepo = new InMemoryMembershipRepository();
        var principalAccessor = new TestPrincipalAccessor();
        var actorId = Guid.NewGuid();

        var create = new CreateGroupUseCase(groupRepo, memRepo, principalAccessor);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            create.CreateAsync(new CreateGroupReq("", null, "default"), actorId, default));

        var group = await create.CreateAsync(new CreateGroupReq("Security", null, "security"), actorId, default);
        Assert.Equal("Security", group.Name);

        await Assert.ThrowsAsync<ConflictException>(() =>
            create.CreateAsync(new CreateGroupReq("Security", null, "security"), actorId, default));
    }

    [Fact]
    public async Task MembershipUseCase_SoleOwnerRemoval_ThrowsConflict()
    {
        var memRepo = new InMemoryMembershipRepository();
        var principalAccessor = new TestPrincipalAccessor();
        var timeProvider = TimeProvider.System;

        var resourceId = Guid.NewGuid();
        var principalId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        memRepo.ResourceToPrincipal[resourceId] = principalId;
        memRepo.ExistingUsers.Add(ownerId);
        memRepo.Memberships[(ownerId, principalId)] = new Business.Modules.Directory.Domain.Entities.PrincipalMembership
        {
            UserId = ownerId,
            PrincipalId = principalId,
            IsOwner = true
        };

        var useCase = new SetMemberUseCase(memRepo, timeProvider, principalAccessor);

        // Demote sole owner
        await Assert.ThrowsAsync<ConflictException>(() =>
            useCase.SetMemberAsync(PrincipalType.Department, resourceId, ownerId, ownerId, new SetMemberRequest(IsMember: true, IsOwner: false), default));

        // Remove sole owner
        await Assert.ThrowsAsync<ConflictException>(() =>
            useCase.SetMemberAsync(PrincipalType.Department, resourceId, ownerId, ownerId, new SetMemberRequest(IsMember: false, IsOwner: false), default));
    }
}
