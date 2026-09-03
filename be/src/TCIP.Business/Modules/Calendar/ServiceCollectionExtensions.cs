using Microsoft.Extensions.DependencyInjection;
using TCIP.Business.Modules.Calendar.Application.UseCases.Events;
using TCIP.Business.Modules.Calendar.Application.UseCases.Notifications;
using TCIP.Business.Modules.Calendar.Application.UseCases.Occurrences;
using TCIP.Business.Modules.Calendar.Application.UseCases.Reminders;
using TCIP.Business.Modules.Calendar.Domain.Services;

namespace TCIP.Business.Modules.Calendar;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCalendarModule(this IServiceCollection services)
    {
        // Domain services
        services.AddSingleton<IReminderSchedulePlanner, ReminderSchedulePlanner>();
        services.AddSingleton<IReminderDispatchValidator, ReminderDispatchValidator>();

        // Events
        services.AddScoped<ICreateEventUseCase, CreateEventUseCase>();
        services.AddScoped<IGetEventByIdUseCase, GetEventByIdUseCase>();
        services.AddScoped<IUpdateEventUseCase, UpdateEventUseCase>();
        services.AddScoped<ICancelEventUseCase, CancelEventUseCase>();
        services.AddScoped<ISetAudienceUseCase, SetAudienceUseCase>();
        services.AddScoped<IRemoveAudienceUseCase, RemoveAudienceUseCase>();

        // Reminders
        services.AddScoped<IAddReminderRuleUseCase, AddReminderRuleUseCase>();
        services.AddScoped<IUpdateReminderRuleUseCase, UpdateReminderRuleUseCase>();
        services.AddScoped<IDeleteReminderRuleUseCase, DeleteReminderRuleUseCase>();

        // Occurrences
        services.AddScoped<IUpsertOccurrenceExceptionUseCase, UpsertOccurrenceExceptionUseCase>();
        services.AddScoped<IDeleteOccurrenceExceptionUseCase, DeleteOccurrenceExceptionUseCase>();
        services.AddScoped<IGetEventsByDayUseCase, GetEventsByDayUseCase>();

        // Notifications
        services.AddScoped<IGetNotificationsUseCase, GetNotificationsUseCase>();
        services.AddScoped<IMarkNotificationReadUseCase, MarkNotificationReadUseCase>();

        return services;
    }
}
