namespace TCIP.Business.Common.Ports;

public sealed record UserPrincipalInfo(
    Guid UserId,
    Guid PrincipalId,
    string? TimeZoneId,
    string? Language);

public interface IUserPrincipalLookupQuery
{
    Task<UserPrincipalInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
