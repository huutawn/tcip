using TCIP.Business.Modules.Calendar.Domain.Entities;

namespace TCIP.Business.Modules.Calendar.Application.Ports;

public interface IEventRepository
{
    Task AddEventAsync(Event calendarEvent, CancellationToken cancellationToken = default);
    Task<Event?> GetEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Event?> GetEventForUpdateAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
