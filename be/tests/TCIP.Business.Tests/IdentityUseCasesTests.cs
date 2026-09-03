using TCIP.Business.Modules.Identity.Application.Contracts;
using TCIP.Business.Modules.Identity.Application.UseCases.Auth;
using TCIP.Business.Modules.Identity.Application.UseCases.Users;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Enums;
using TCIP.Business.Tests.TestDoubles;
using TCIP.Common.Exceptions;
using Xunit;

namespace TCIP.Business.Tests;

public sealed class IdentityUseCasesTests
{
    [Fact]
    public async Task RegisterUser_DuplicateEmail_ThrowsConflict()
    {
        var userRepo = new InMemoryUserRepository();
        var hasher = new SimplePasswordHasher();
        var useCase = new RegisterUserUseCase(userRepo, hasher, TimeProvider.System);

        var req = new RegisterRequest("test@example.com", "secret123", "Test User");
        var res = await useCase.RegisterAsync(req);
        Assert.Equal("test@example.com", res.Email);
        Assert.Equal("Test User", res.DisplayName);

        await Assert.ThrowsAsync<ConflictException>(() => useCase.RegisterAsync(req));
    }

    [Fact]
    public async Task LoginUser_InvalidCredentials_ThrowsUnauthentication()
    {
        var userRepo = new InMemoryUserRepository();
        var sessionRepo = new InMemorySessionRepository();
        var hasher = new SimplePasswordHasher();
        var tokenIssuer = new SimpleTokenIssuer();
        var refreshGen = new SimpleRefreshTokenGenerator();
        var config = new SimpleIdentityConfiguration();
        var timeProvider = TimeProvider.System;

        var loginUseCase = new LoginUserUseCase(userRepo, sessionRepo, hasher, tokenIssuer, refreshGen, config, timeProvider);

        // User does not exist
        await Assert.ThrowsAsync<UnauthenticationException>(() =>
            loginUseCase.LoginAsync(new LoginRequest("missing@example.com", "pwd")));

        // User exists with different password
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = hasher.HashPassword(null!, "correctpwd")
        };
        await userRepo.CreateAsync(user);

        await Assert.ThrowsAsync<UnauthenticationException>(() =>
            loginUseCase.LoginAsync(new LoginRequest("user@example.com", "wrongpwd")));

        // Valid credentials
        var response = await loginUseCase.LoginAsync(new LoginRequest("user@example.com", "correctpwd"));
        Assert.NotNull(response.AccessToken);
        Assert.NotNull(response.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_InvalidOrExpired_ThrowsUnauthentication()
    {
        var userRepo = new InMemoryUserRepository();
        var sessionRepo = new InMemorySessionRepository();
        var tokenIssuer = new SimpleTokenIssuer();
        var refreshGen = new SimpleRefreshTokenGenerator();
        var config = new SimpleIdentityConfiguration();
        var timeProvider = TimeProvider.System;

        var refreshUseCase = new RefreshTokenUseCase(sessionRepo, tokenIssuer, refreshGen, config, timeProvider);

        // Unknown token
        await Assert.ThrowsAsync<UnauthenticationException>(() =>
            refreshUseCase.RefreshAsync(new RefreshTokenRequest("nonexistent")));

        // Expired session
        var now = timeProvider.GetUtcNow();
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.com" };
        var rawToken = "my_token";
        var tokenHash = refreshGen.Hash(rawToken);
        var expiredSession = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            RefreshTokenHash = tokenHash,
            ExpiresAtUtc = now.AddMinutes(-5)
        };
        await sessionRepo.CreateAsync(expiredSession);

        await Assert.ThrowsAsync<UnauthenticationException>(() =>
            refreshUseCase.RefreshAsync(new RefreshTokenRequest(rawToken)));
    }

    [Fact]
    public async Task UpdateUserRole_AdminSelfDemotion_ThrowsConflict()
    {
        var userRepo = new InMemoryUserRepository();
        var timeProvider = TimeProvider.System;

        var adminId = Guid.NewGuid();
        var admin = new User { Id = adminId, Email = "admin@example.com", Role = UserRole.Admin };
        await userRepo.CreateAsync(admin);

        var useCase = new UpdateUserRoleUseCase(userRepo, timeProvider);

        await Assert.ThrowsAsync<ConflictException>(() =>
            useCase.UpdateRoleAsync(adminId, adminId, new UpdateUserRoleRequest(UserRole.User)));

        var otherUserId = Guid.NewGuid();
        var otherUser = new User { Id = otherUserId, Email = "other@example.com", Role = UserRole.User };
        await userRepo.CreateAsync(otherUser);

        var updated = await useCase.UpdateRoleAsync(adminId, otherUserId, new UpdateUserRoleRequest(UserRole.Admin));
        Assert.True(updated);
        Assert.Equal(UserRole.Admin, otherUser.Role);
    }

    [Fact]
    public async Task GetUserById_AndPage_ReturnsCorrectData()
    {
        var userRepo = new InMemoryUserRepository();
        var user = new User
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            Email = "one@test.com",
            DisplayName = "User One",
            Role = UserRole.User
        };
        await userRepo.CreateAsync(user);

        var getUseCase = new GetUserByIdUseCase(userRepo);
        var pageUseCase = new GetUsersPageUseCase(userRepo);

        Assert.Null(await getUseCase.GetByIdAsync(Guid.NewGuid()));
        var found = await getUseCase.GetByIdAsync(user.Id);
        Assert.NotNull(found);
        Assert.Equal("User One", found.DisplayName);

        var page = await pageUseCase.GetPageAsync(new UserListQuery(1, 10));
        Assert.Single(page.Items);
        Assert.Equal(1, page.TotalCount);
    }
}
