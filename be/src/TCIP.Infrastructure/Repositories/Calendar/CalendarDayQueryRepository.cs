using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Enums;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.Calendar;

public sealed class CalendarDayQueryRepository(TcipDbContext dbContext) : ICalendarDayQueryRepository
{
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
}
