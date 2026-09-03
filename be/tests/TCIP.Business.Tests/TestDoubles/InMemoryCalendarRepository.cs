using TCIP.Business.Common.Ports;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Entities;

namespace TCIP.Business.Tests.TestDoubles;

public sealed class InMemoryCalendarRepository :
    IEventRepository,
    ICalendarDayQueryRepository,
    INotificationRepository,
    IPrincipalAvailabilityQuery,
    IUserPrincipalLookupQuery
{
    public readonly Dictionary<Guid, Event> Events = new();
    public readonly Dictionary<Guid, User> Users = new();
    public readonly Dictionary<Guid, Principal> Principals = new();
    public readonly Dictionary<Guid, Notification> Notifications = new();
    public readonly List<PrincipalMembership> Memberships = new();

    public Task AddEventAsync(Event calendarEvent, CancellationToken cancellationToken = default)
    {
        Events[calendarEvent.Id] = calendarEvent;
        return Task.CompletedTask;
    }

    public Task<Event?> GetEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        Events.TryGetValue(eventId, out var ev);
        return Task.FromResult(ev);
    }

    public Task<Event?> GetEventForUpdateAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        Events.TryGetValue(eventId, out var ev);
        return Task.FromResult(ev);
    }

    public Task<UserPrincipalInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (Users.TryGetValue(userId, out var user))
        {
            return Task.FromResult<UserPrincipalInfo?>(new UserPrincipalInfo(user.Id, user.PrincipalId, user.TimeZoneId, user.Language));
        }
        return Task.FromResult<UserPrincipalInfo?>(null);
    }

    public Task<IReadOnlyList<Event>> GetEventsForDayWindowAsync(
        Guid userId,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        CancellationToken cancellationToken = default)
    {
        Users.TryGetValue(userId, out var user);
        var userPrincipalId = user?.PrincipalId ?? Guid.Empty;

        var matching = Events.Values
            .Where(x => x.Status == EventStatus.Active &&
                (x.CreatedById == userId ||
                 x.Audiences.Any(a => a.Status == EventAudienceStatus.Active &&
                    (a.PrincipalId == userPrincipalId ||
                     Memberships.Any(pm => pm.PrincipalId == a.PrincipalId && pm.UserId == userId && pm.LeftAtUtc == null)))))
            .Where(x => x.RecurrenceRule != null ||
                (x.StartAtUtc < windowEndUtc && (x.EndAtUtc == null ? x.StartAtUtc >= windowStartUtc : x.EndAtUtc > windowStartUtc)))
            .ToList();

        return Task.FromResult<IReadOnlyList<Event>>(matching);
    }

    public Task<IReadOnlyList<Notification>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = Notifications.Values
            .Where(x => x.RecipientUserId == userId)
            .OrderByDescending(x => x.SentAtUtc)
            .ToList();
        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }

    public Task<Notification?> GetNotificationForUpdateAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        Notifications.TryGetValue(notificationId, out var notif);
        if (notif?.RecipientUserId != userId) return Task.FromResult<Notification?>(null);
        return Task.FromResult<Notification?>(notif);
    }

    public Task<bool> ArePrincipalsAvailableAsync(IReadOnlyCollection<Guid> principalIds, CancellationToken cancellationToken = default)
    {
        if (principalIds.Count == 0) return Task.FromResult(true);
        var count = principalIds.Distinct().Count(id => Principals.TryGetValue(id, out var p) && p.Available);
        return Task.FromResult(count == principalIds.Distinct().Count());
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
