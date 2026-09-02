using TCIP.Business.Modules.Identity.Application.Contracts;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Identity.Application.UseCases;

public interface IUserUseCase
{
    Task<UserResponse?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PagedUsersResponse> GetPageAsync(UserListQuery query, CancellationToken cancellationToken = default);
    Task<bool> UpdateRoleAsync(Guid actorUserId, Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);
}

public sealed class UserUseCase(IUserRepository userRepository, TimeProvider timeProvider) : IUserUseCase
{
    public async Task<UserResponse?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task<PagedUsersResponse> GetPageAsync(
        UserListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (users, totalCount) = await userRepository.GetPageAsync(
            (query.Page - 1) * query.PageSize,
            query.PageSize,
            cancellationToken);

        return new PagedUsersResponse(
            users.Select(Map).ToArray(),
            query.Page,
            query.PageSize,
            totalCount);
    }

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

    private static UserResponse Map(User user) => new(
        user.Id,
        user.PrincipalId,
        user.Email,
        user.DisplayName,
        user.EmailVerified,
        user.Language,
        user.TimeZoneId,
        user.Role,
        user.CreatedAtUtc);
}
