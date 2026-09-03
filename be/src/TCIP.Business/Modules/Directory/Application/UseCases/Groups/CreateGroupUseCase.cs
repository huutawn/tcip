using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Business.Modules.Directory.Application.Contracts;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Directory.Application.UseCases.Groups;

public interface ICreateGroupUseCase
{
    Task<GroupResponse> CreateAsync(CreateGroupReq request, Guid actorUserId, CancellationToken cancellationToken);
}

public sealed class CreateGroupUseCase(
    IGroupRepository groupRepository,
    IMembershipRepository membershipRepository,
    ICurrentPrincipalAccessor principalAccessor) : ICreateGroupUseCase
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
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Type = type
        };

        await groupRepository.AddAsync(group, cancellationToken);
        return new GroupResponse(group.Id, group.PrincipalId, group.Name, group.Description, group.Type);
    }
}
