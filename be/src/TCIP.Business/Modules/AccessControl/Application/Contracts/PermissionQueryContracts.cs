namespace TCIP.Business.Modules.AccessControl.Application.Contracts;

public sealed record PrincipalPermissionsResponse(
    Guid PrincipalId,
    Guid? ResourcePrincipalId,
    IReadOnlyList<string> Permissions);

public sealed record CheckPermissionResponse(
    Guid PrincipalId,
    string Permission,
    Guid? ResourcePrincipalId,
    bool Allowed);
