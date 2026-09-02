namespace TCIP.Business.Common.Ports;

public interface ICurrentPrincipalAccessor
{
    Guid? GetCurrentUserId();
    bool IsAdmin();
    bool HasGlobalPermission(string permission);
    bool IsResourceOwner(Guid resourcePrincipalId);
    bool HasResourcePermission(Guid resourcePrincipalId, string permission);
    HashSet<string> GetResourcePermissions(Guid resourcePrincipalId);
}
