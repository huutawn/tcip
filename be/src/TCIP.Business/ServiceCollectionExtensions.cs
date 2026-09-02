using Microsoft.Extensions.DependencyInjection;
using TCIP.Business.Modules.AccessControl.Application.UseCases;
using TCIP.Business.Modules.Calendar.Application.UseCases;
using TCIP.Business.Modules.Calendar.Domain.Services;
using TCIP.Business.Modules.Directory.Application.UseCases;
using TCIP.Business.Modules.Identity.Application.UseCases;

namespace TCIP.Business;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        // Identity UseCases
        services.AddScoped<IAuthUseCase, AuthUseCase>();
        services.AddScoped<IUserUseCase, UserUseCase>();

        // Directory UseCases
        services.AddScoped<IDepartmentUseCase, DepartmentUseCase>();
        services.AddScoped<IGroupUseCase, GroupUseCase>();
        services.AddScoped<ITeamUseCase, TeamUseCase>();
        services.AddScoped<IProjectUseCase, ProjectUseCase>();
        services.AddScoped<IMembershipUseCase, MembershipUseCase>();

        // AccessControl UseCases
        services.AddScoped<IRbacUseCase, RbacUseCase>();

        // Calendar Domain Services & UseCases
        services.AddSingleton<IReminderSchedulePlanner, ReminderSchedulePlanner>();
        services.AddSingleton<IReminderDispatchValidator, ReminderDispatchValidator>();
        services.AddScoped<IEventCommandUseCase, EventCommandUseCase>();
        services.AddScoped<IReminderRuleUseCase, ReminderRuleUseCase>();
        services.AddScoped<IEventOccurrenceUseCase, EventOccurrenceUseCase>();
        services.AddScoped<INotificationQueryUseCase, NotificationQueryUseCase>();

        return services;
    }
}
