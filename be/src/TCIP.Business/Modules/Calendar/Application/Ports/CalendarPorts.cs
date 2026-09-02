using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Entities;

namespace TCIP.Business.Modules.Calendar.Application.Ports;

public interface ICalendarRepository
{
    Task AddEventAsync(Event calendarEvent, CancellationToken cancellationToken = default);
    Task<Event?> GetEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Event?> GetEventForUpdateAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetEventsForDayWindowAsync(Guid userId, DateTimeOffset windowStartUtc, DateTimeOffset windowEndUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetNotificationForUpdateAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<bool> PrincipalsExistAndAvailableAsync(IReadOnlyCollection<Guid> principalIds, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IAudienceRecipientResolver
{
    Task<IReadOnlyList<Guid>> GetRecipientsForEventAsync(
        Guid eventId,
        DateTimeOffset resolvedAtUtc,
        Guid? cursor,
        int limit,
        CancellationToken cancellationToken = default);
}

public interface INotificationGateway
{
    Task SendNotificationToUserAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken = default);
}
