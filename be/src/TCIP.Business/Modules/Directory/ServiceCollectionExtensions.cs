using Microsoft.Extensions.DependencyInjection;
using TCIP.Business.Modules.Directory.Application.UseCases.Departments;
using TCIP.Business.Modules.Directory.Application.UseCases.Groups;
using TCIP.Business.Modules.Directory.Application.UseCases.Memberships;
using TCIP.Business.Modules.Directory.Application.UseCases.Projects;
using TCIP.Business.Modules.Directory.Application.UseCases.Teams;

namespace TCIP.Business.Modules.Directory;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDirectoryModule(this IServiceCollection services)
    {
        // Departments
        services.AddScoped<ICreateDepartmentUseCase, CreateDepartmentUseCase>();
        services.AddScoped<IGetDepartmentByIdUseCase, GetDepartmentByIdUseCase>();
        services.AddScoped<IUpdateDepartmentUseCase, UpdateDepartmentUseCase>();
        services.AddScoped<IDeleteDepartmentUseCase, DeleteDepartmentUseCase>();

        // Teams
        services.AddScoped<ICreateTeamUseCase, CreateTeamUseCase>();
        services.AddScoped<IGetTeamByIdUseCase, GetTeamByIdUseCase>();
        services.AddScoped<IUpdateTeamUseCase, UpdateTeamUseCase>();
        services.AddScoped<IDeleteTeamUseCase, DeleteTeamUseCase>();

        // Projects
        services.AddScoped<ICreateProjectUseCase, CreateProjectUseCase>();
        services.AddScoped<IGetProjectByIdUseCase, GetProjectByIdUseCase>();
        services.AddScoped<IUpdateProjectUseCase, UpdateProjectUseCase>();
        services.AddScoped<IDeleteProjectUseCase, DeleteProjectUseCase>();

        // Groups
        services.AddScoped<ICreateGroupUseCase, CreateGroupUseCase>();

        // Memberships
        services.AddScoped<IGetMembersUseCase, GetMembersUseCase>();
        services.AddScoped<ISetMemberUseCase, SetMemberUseCase>();

        return services;
    }
}
