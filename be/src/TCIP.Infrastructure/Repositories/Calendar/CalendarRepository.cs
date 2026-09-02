using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.Calendar;

public sealed class CalendarRepository(TcipDbContext dbContext) : ICalendarRepository
{
    public async Task AddEventAsync(Event calendarEvent, CancellationToken cancellationToken = default)
    {
        dbContext.Events.Add(calendarEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Event?> GetEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        dbContext.Events
            .AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.Audiences)
                .ThenInclude(x => x.Principal)
                    .ThenInclude(p => p.User)
            .Include(x => x.Audiences)
                .ThenInclude(x => x.Principal)
                    .ThenInclude(p => p.Group)
            .Include(x => x.Audiences)
                .ThenInclude(x => x.Principal)
                    .ThenInclude(p => p.Team)
            .Include(x => x.Audiences)
                .ThenInclude(x => x.Principal)
                    .ThenInclude(p => p.Project)
            .Include(x => x.Audiences)
                .ThenInclude(x => x.Principal)
                    .ThenInclude(p => p.Department)
            .Include(x => x.ReminderRules)
            .Include(x => x.OccurrenceExceptions)
            .SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);

    public Task<Event?> GetEventForUpdateAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        dbContext.Events
            .Include(x => x.Translations)
            .Include(x => x.Audiences)
            .Include(x => x.ReminderRules)
                .ThenInclude(x => x.Schedule)
            .Include(x => x.OccurrenceExceptions)
            .SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);

    public Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public async Task<IReadOnlyList<Event>> GetEventsForDayWindowAsync(
        Guid userId,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return [];

        var userPrincipalId = user.PrincipalId;

        var candidateEvents = await dbContext.Events
            .AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.OccurrenceExceptions)
            .Where(x => x.Status == EventStatus.Active &&
                (x.CreatedById == userId ||
                 x.Audiences.Any(a => a.Status == EventAudienceStatus.Active &&
                    (a.PrincipalId == userPrincipalId ||
                     dbContext.PrincipalMemberships.Any(pm => pm.PrincipalId == a.PrincipalId && pm.UserId == userId && pm.LeftAtUtc == null)))))
            .Where(x => x.RecurrenceRule != null ||
                (x.StartAtUtc < windowEndUtc && (x.EndAtUtc == null ? x.StartAtUtc >= windowStartUtc : x.EndAtUtc > windowStartUtc)))
            .ToListAsync(cancellationToken);

        return candidateEvents;
    }

    public async Task<IReadOnlyList<Notification>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .AsNoTracking()
            .Where(x => x.RecipientUserId == userId)
            .OrderByDescending(x => x.SentAtUtc)
            .ToListAsync(cancellationToken);

    public Task<Notification?> GetNotificationForUpdateAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default) =>
        dbContext.Notifications.SingleOrDefaultAsync(
            x => x.Id == notificationId && x.RecipientUserId == userId,
            cancellationToken);

    public async Task<bool> PrincipalsExistAndAvailableAsync(IReadOnlyCollection<Guid> principalIds, CancellationToken cancellationToken = default)
    {
        if (principalIds.Count == 0)
            return true;

        var count = await dbContext.Principals.CountAsync(p => principalIds.Contains(p.Id) && p.Available, cancellationToken);
        return count == principalIds.Distinct().Count();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
