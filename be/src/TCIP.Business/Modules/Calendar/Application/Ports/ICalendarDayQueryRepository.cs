using TCIP.Business.Modules.Calendar.Domain.Entities;

namespace TCIP.Business.Modules.Calendar.Application.Ports;

public interface ICalendarDayQueryRepository
{
    Task<IReadOnlyList<Event>> GetEventsForDayWindowAsync(
        Guid userId,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        CancellationToken cancellationToken = default);
}
