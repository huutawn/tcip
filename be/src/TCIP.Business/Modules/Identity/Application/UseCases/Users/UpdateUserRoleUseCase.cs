using TCIP.Business.Modules.Identity.Application.Contracts;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Business.Modules.Identity.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Identity.Application.UseCases.Users;

public interface IUpdateUserRoleUseCase
{
    Task<bool> UpdateRoleAsync(Guid actorUserId, Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);
}

public sealed class UpdateUserRoleUseCase(IUserRepository userRepository, TimeProvider timeProvider) : IUpdateUserRoleUseCase
{
    public Task<bool> UpdateRoleAsync(
        Guid actorUserId,
        Guid userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == userId && request.Role != UserRole.Admin)
        {
            throw new ConflictException("Administrators cannot remove their own admin role.");
        }

        return userRepository.UpdateRoleAsync(
            userId,
            request.Role,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }
}
