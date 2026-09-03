using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.Calendar.Domain.Services;

public static class EventAccessValidator
{
    public static Event ValidateOwnerAndVersion(
        Event? calendarEvent,
        Guid eventId,
        Guid actorUserId,
        long expectedVersion,
        string forbiddenMessage)
    {
        if (calendarEvent is null)
        {
            throw new NotFoundException($"Event with ID '{eventId}' not found.");
        }

        if (calendarEvent.CreatedById != actorUserId)
        {
            throw new ForbiddenException(forbiddenMessage);
        }

        if (calendarEvent.Version != expectedVersion)
        {
            throw new PreconditionFailedException($"Precondition Failed: Resource version '{calendarEvent.Version}' does not match expected version '{expectedVersion}'.");
        }

        return calendarEvent;
    }
}
