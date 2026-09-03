using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.Calendar.Application.Ports;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.Calendar;

public sealed class EventRepository(TcipDbContext dbContext) : IEventRepository
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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
