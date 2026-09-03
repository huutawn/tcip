using TCIP.Business.Modules.Identity.Application.Contracts;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Business.Modules.Identity.Domain.Entities;

namespace TCIP.Business.Modules.Identity.Application.UseCases.Users;

public interface IGetUserByIdUseCase
{
    Task<UserResponse?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class GetUserByIdUseCase(IUserRepository userRepository) : IGetUserByIdUseCase
{
    public async Task<UserResponse?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : Map(user);
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
