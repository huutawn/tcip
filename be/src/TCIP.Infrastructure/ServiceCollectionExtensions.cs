using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Identity.Application.Ports;
using TCIP.Infrastructure.Adapters.Hub;
using TCIP.Infrastructure.Adapters.Recurrence;
using TCIP.Infrastructure.Adapters.Security;
using TCIP.Infrastructure.Data;
using TCIP.Infrastructure.Messaging;
using TCIP.Infrastructure.Repositories.AccessControl;
using TCIP.Infrastructure.Repositories.Calendar;
using TCIP.Infrastructure.Repositories.Directory;
using TCIP.Infrastructure.Repositories.Identity;
using TCIP.Infrastructure.Services;
using TCIP.Infrastructure.Workers;

namespace TCIP.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<TcipDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<DirectoryRecipientResolver>();
        services.AddScoped<IDirectoryRecipientResolver>(sp => sp.GetRequiredService<DirectoryRecipientResolver>());
        services.AddScoped<IAudienceRecipientResolver>(sp => sp.GetRequiredService<DirectoryRecipientResolver>());
        services.AddScoped<IRbacRepository, RbacRepository>();
        services.AddScoped<ICalendarRepository, CalendarRepository>();

        // Adapters
        services.AddSingleton<IRecurrenceEngine, RecurrenceEngine>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<IIdentityConfiguration, IdentityConfigurationAdapter>();
        services.AddScoped<ICurrentPrincipalAccessor, CurrentPrincipalAccessor>();
        services.AddScoped<INotificationGateway, SignalRNotificationGateway>();
        services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();

        // Services
        services.AddScoped<INotificationDeliveryService, NotificationDeliveryService>();
        services.AddSingleton(TimeProvider.System);

        // Kafka and background workers
        if (KafkaConfiguration.Enabled(configuration))
        {
            services.AddSingleton<IProducer<string, string>>(_ =>
                new ProducerBuilder<string, string>(new ProducerConfig
                {
                    BootstrapServers = KafkaConfiguration.BootstrapServers(configuration),
                    EnableIdempotence = true
                }).Build());

            services.AddHostedService<ReminderSchedulerWorker>();
            services.AddHostedService<OutboxPublisherWorker>();
            services.AddHostedService<AudienceResolverWorker>();
            services.AddHostedService<NotificationBatchDeliveryWorker>();
        }

        return services;
    }
}
