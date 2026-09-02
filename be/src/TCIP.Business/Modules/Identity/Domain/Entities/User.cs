using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Enums;

namespace TCIP.Business.Modules.Identity.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public Guid PrincipalId { get; set; }
    public Principal Principal { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Language { get; set; } = "en";
    public string TimeZoneId { get; set; } = "UTC";
    public bool EmailVerified { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
