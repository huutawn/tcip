namespace TCIP.Business.Common.Ports;

public interface IPrincipalAvailabilityQuery
{
    Task<bool> ArePrincipalsAvailableAsync(IReadOnlyCollection<Guid> principalIds, CancellationToken cancellationToken = default);
}
