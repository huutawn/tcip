using Microsoft.Extensions.DependencyInjection;
using TCIP.Business.Modules.Identity.Application.UseCases.Auth;
using TCIP.Business.Modules.Identity.Application.UseCases.Users;

namespace TCIP.Business.Modules.Identity;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<ILoginUserUseCase, LoginUserUseCase>();
        services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();

        services.AddScoped<IGetUserByIdUseCase, GetUserByIdUseCase>();
        services.AddScoped<IGetUsersPageUseCase, GetUsersPageUseCase>();
        services.AddScoped<IUpdateUserRoleUseCase, UpdateUserRoleUseCase>();

        return services;
    }
}
