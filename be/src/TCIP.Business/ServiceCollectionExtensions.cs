using Microsoft.Extensions.DependencyInjection;
using TCIP.Business.Modules.AccessControl;
using TCIP.Business.Modules.Calendar;
using TCIP.Business.Modules.Directory;
using TCIP.Business.Modules.Identity;

namespace TCIP.Business;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        services.AddIdentityModule();
        services.AddAccessControlModule();
        services.AddDirectoryModule();
        services.AddCalendarModule();

        return services;
    }
}
