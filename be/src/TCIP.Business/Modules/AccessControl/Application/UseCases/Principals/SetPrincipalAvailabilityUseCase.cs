using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Common.Exceptions;

namespace TCIP.Business.Modules.AccessControl.Application.UseCases.Principals;

public interface ISetPrincipalAvailabilityUseCase
{
    Task SetPrincipalAvailabilityAsync(Guid principalId, bool available, CancellationToken cancellationToken = default);
}

public sealed class SetPrincipalAvailabilityUseCase(IPrincipalRepository principalRepository) : ISetPrincipalAvailabilityUseCase
{
    public async Task SetPrincipalAvailabilityAsync(
        Guid principalId,
        bool available,
        CancellationToken cancellationToken = default)
    {
        var principal = await principalRepository.GetPrincipalByIdAsync(principalId, cancellationToken)
            ?? throw new NotFoundException("Principal not found.");
        principal.Available = available;
        await principalRepository.SaveChangesAsync(cancellationToken);
    }
}
