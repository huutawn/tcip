namespace TCIP.Business.Modules.AccessControl.Application.Contracts;

public sealed record PrincipalResponse(
    Guid PrincipalId,
    string Type,
    string Name,
    string? Description,
    string? Email,
    bool Available);

public sealed record PrincipalSearchQuery(
    string? Search = null,
    string? Type = null,
    bool? Available = null,
    string? Cursor = null,
    int Limit = 20);

public sealed record PrincipalSearchResponse(
    IReadOnlyList<PrincipalResponse> Items,
    string? NextCursor);

public sealed record SetPrincipalAvailabilityRequest(
    bool Available);
