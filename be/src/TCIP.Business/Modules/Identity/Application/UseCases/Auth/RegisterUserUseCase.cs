using TCIP.Business.Modules.Identity.Application.Contracts;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Enums;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Identity.Application.UseCases.Auth;

public interface IRegisterUserUseCase
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}

public sealed class RegisterUserUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IRegisterUserUseCase
{
    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await userRepository.ExistsByEmailAsync(email, cancellationToken);
        if (exists)
        {
            throw new ConflictException("Email already exists.");
        }

        var now = timeProvider.GetUtcNow();
        var principalId = Guid.NewGuid();

        var user = new User
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal
            {
                Id = principalId,
                Type = PrincipalType.User
            },
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            EmailVerified = false,
            Role = UserRole.User,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await userRepository.CreateAsync(user, cancellationToken);

        return new RegisterResponse(
            user.Id,
            user.PrincipalId,
            user.Email,
            user.DisplayName,
            user.CreatedAtUtc);
    }
}
