using TCIP.Business.Modules.Directory.Domain.Enums;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Modules.AccessControl.Domain.Entities;

namespace TCIP.Business.Modules.Directory.Domain.Entities;

public sealed class Principal
{
    public Guid Id { get; set; }
    public PrincipalType Type { get; set; }
    public bool Available { get; set; } = true;
    public User? User { get; set; }
    public Group? Group { get; set; }
    public Team? Team { get; set; }
    public Project? Project { get; set; }
    public Department? Department { get; set; }
    public ICollection<RoleAssignment> SubjectAssignments { get; set; } = new List<RoleAssignment>();
}

public sealed class Department
{
    public Guid Id { get; set; }
    public Guid PrincipalId { get; set; }
    public Principal Principal { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class Group
{
    public Guid Id { get; set; }
    public Guid PrincipalId { get; set; }
    public Principal Principal { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Type { get; set; } = null!;
}

public sealed class Team
{
    public Guid Id { get; set; }
    public Guid PrincipalId { get; set; }
    public Principal Principal { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid PrincipalId { get; set; }
    public Principal Principal { get; set; } = null!;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public string Type { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ProjectTranslation
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Language { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public sealed class Position
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public sealed class UserPosition
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
}

public sealed class PrincipalMembership
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid PrincipalId { get; set; }
    public Principal Principal { get; set; } = null!;
    public bool IsOwner { get; set; }
    public DateTimeOffset JoinedAtUtc { get; set; }
    public DateTimeOffset? LeftAtUtc { get; set; }
}
