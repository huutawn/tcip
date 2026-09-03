using TCIP.Business.Modules.Identity.Application.Contracts;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Business.Modules.Identity.Domain.Entities;

namespace TCIP.Business.Modules.Identity.Application.UseCases.Users;

public interface IGetUsersPageUseCase
{
    Task<PagedUsersResponse> GetPageAsync(UserListQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetUsersPageUseCase(IUserRepository userRepository) : IGetUsersPageUseCase
{
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
