using Microsoft.Extensions.DependencyInjection;
using TCIP.Business.Modules.AccessControl.Application.UseCases.PermissionQueries;
using TCIP.Business.Modules.AccessControl.Application.UseCases.Permissions;
using TCIP.Business.Modules.AccessControl.Application.UseCases.Principals;
using TCIP.Business.Modules.AccessControl.Application.UseCases.RoleAssignments;
using TCIP.Business.Modules.AccessControl.Application.UseCases.Roles;

namespace TCIP.Business.Modules.AccessControl;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccessControlModule(this IServiceCollection services)
    {
        // Principals
        services.AddScoped<IGetPrincipalByIdUseCase, GetPrincipalByIdUseCase>();
        services.AddScoped<ISearchPrincipalsUseCase, SearchPrincipalsUseCase>();
        services.AddScoped<ISetPrincipalAvailabilityUseCase, SetPrincipalAvailabilityUseCase>();

        // Permissions
        services.AddScoped<ICreatePermissionUseCase, CreatePermissionUseCase>();
        services.AddScoped<IGetPermissionByIdUseCase, GetPermissionByIdUseCase>();
        services.AddScoped<IGetAllPermissionsUseCase, GetAllPermissionsUseCase>();
        services.AddScoped<IDeletePermissionUseCase, DeletePermissionUseCase>();

        // Roles
        services.AddScoped<ICreateRoleUseCase, CreateRoleUseCase>();
        services.AddScoped<IGetRoleByIdUseCase, GetRoleByIdUseCase>();
        services.AddScoped<IGetAllRolesUseCase, GetAllRolesUseCase>();
        services.AddScoped<IAssignPermissionsToRoleUseCase, AssignPermissionsToRoleUseCase>();
        services.AddScoped<IDeleteRoleUseCase, DeleteRoleUseCase>();

        // Role Assignments
        services.AddScoped<ICreateRoleAssignmentUseCase, CreateRoleAssignmentUseCase>();
        services.AddScoped<IGetRoleAssignmentsByPrincipalIdUseCase, GetRoleAssignmentsByPrincipalIdUseCase>();
        services.AddScoped<IGetRoleAssignmentsByRoleIdUseCase, GetRoleAssignmentsByRoleIdUseCase>();
        services.AddScoped<IDeleteRoleAssignmentUseCase, DeleteRoleAssignmentUseCase>();

        // Permission Queries
        services.AddScoped<IGetPermissionsForPrincipalUseCase, GetPermissionsForPrincipalUseCase>();
        services.AddScoped<ICheckPermissionUseCase, CheckPermissionUseCase>();

        return services;
    }
}
