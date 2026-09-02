using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCIP.Business.Modules.Directory.Domain.Entities;

namespace TCIP.Infrastructure.Data.Configurations;

public sealed class PrincipalConfiguration : IEntityTypeConfiguration<Principal>
{
    public void Configure(EntityTypeBuilder<Principal> builder)
    {
        builder.ToTable("principals");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasMaxLength(100);
        builder.Property(p => p.Available).HasColumnName("available").HasDefaultValue(true).IsRequired();
        builder.HasIndex(p => p.Type);
        builder.HasIndex(p => new { p.Available, p.Id });
    }
}

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PrincipalId).HasColumnName("principal_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne(x => x.Principal)
            .WithOne(x => x.Department)
            .HasForeignKey<Department>(x => x.PrincipalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id");
        builder.Property(g => g.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(g => g.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(g => g.Type).HasColumnName("type").IsRequired().HasMaxLength(64);
        builder.Property(g => g.PrincipalId).HasColumnName("principal_id");

        builder.HasOne(g => g.Principal)
            .WithOne(p => p.Group)
            .HasForeignKey<Group>(g => g.PrincipalId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => new { g.Name, g.Type }).IsUnique();
    }
}

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PrincipalId).HasColumnName("principal_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne(x => x.Principal)
            .WithOne(x => x.Team)
            .HasForeignKey<Team>(x => x.PrincipalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(p => p.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
        builder.Property(p => p.PrincipalId).HasColumnName("principal_id").IsRequired();
        builder.Property(p => p.OwnerId).HasColumnName("owner_id").IsRequired();
        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne(p => p.Principal)
            .WithOne(p => p.Project)
            .HasForeignKey<Project>(p => p.PrincipalId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProjectTranslationConfiguration : IEntityTypeConfiguration<ProjectTranslation>
{
    public void Configure(EntityTypeBuilder<ProjectTranslation> builder)
    {
        builder.ToTable("project_translations");
        builder.HasKey(x => new { x.ProjectId, x.Language });
        builder.Property(x => x.ProjectId).HasColumnName("project_id");
        builder.Property(x => x.Language).HasColumnName("language").HasMaxLength(16);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");

        builder.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class UserPositionConfiguration : IEntityTypeConfiguration<UserPosition>
{
    public void Configure(EntityTypeBuilder<UserPosition> builder)
    {
        builder.ToTable("user_positions");
        builder.HasKey(x => new { x.UserId, x.PositionId });
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.PositionId).HasColumnName("position_id");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PrincipalMembershipConfiguration : IEntityTypeConfiguration<PrincipalMembership>
{
    public void Configure(EntityTypeBuilder<PrincipalMembership> builder)
    {
        builder.ToTable("principal_memberships");
        builder.HasKey(x => new { x.UserId, x.PrincipalId });
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.PrincipalId).HasColumnName("principal_id");
        builder.Property(x => x.IsOwner).HasColumnName("is_owner").IsRequired();
        builder.Property(x => x.JoinedAtUtc).HasColumnName("joined_at_utc").IsRequired();
        builder.Property(x => x.LeftAtUtc).HasColumnName("left_at_utc");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Principal).WithMany().HasForeignKey(x => x.PrincipalId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.PrincipalId, x.LeftAtUtc });
    }
}
