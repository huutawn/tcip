namespace TCIP.Business.Common.Ports;

public interface IUserAudienceMembershipQuery
{
    Task<IReadOnlyList<Guid>> GetActiveAudiencePrincipalIdsForUserAsync(
        Guid userId,
        DateTimeOffset atUtc,
        CancellationToken cancellationToken = default);
}
