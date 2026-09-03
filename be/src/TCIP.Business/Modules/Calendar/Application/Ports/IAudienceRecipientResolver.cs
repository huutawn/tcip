namespace TCIP.Business.Modules.Calendar.Application.Ports;

public interface IAudienceRecipientResolver
{
    Task<IReadOnlyList<Guid>> GetRecipientsForEventAsync(
        Guid eventId,
        DateTimeOffset resolvedAtUtc,
        Guid? cursor,
        int limit,
        CancellationToken cancellationToken = default);
}
