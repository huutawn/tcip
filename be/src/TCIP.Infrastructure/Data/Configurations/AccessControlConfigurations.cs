using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCIP.Business.Modules.AccessControl.Domain.Entities;

namespace TCIP.Infrastructure.Data.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(1000);
        builder.HasIndex(p => p.Name).IsUnique();
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(1000);
        builder.HasIndex(r => r.Name).IsUnique();
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
        builder.Property(rp => rp.RoleId).HasColumnName("role_id");
        builder.Property(rp => rp.PermissionId).HasColumnName("permission_id");

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.ToTable("role_assignments");
        builder.HasKey(ra => ra.Id);
        builder.Property(ra => ra.Id).HasColumnName("id");
        builder.Property(ra => ra.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(ra => ra.SubjectPrincipalId).HasColumnName("subject_principal_id").IsRequired();
        builder.Property(ra => ra.ResourcePrincipalId).HasColumnName("resource_principal_id");
        builder.Property(ra => ra.GrantedByPrincipalId).HasColumnName("granted_by_principal_id");
        builder.Property(ra => ra.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne(ra => ra.Role)
            .WithMany(r => r.SubjectAssignments)
            .HasForeignKey(ra => ra.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ra => ra.SubjectPrincipal)
            .WithMany(p => p.SubjectAssignments)
            .HasForeignKey(ra => ra.SubjectPrincipalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ra => ra.ResourcePrincipal)
            .WithMany()
            .HasForeignKey(ra => ra.ResourcePrincipalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ra => new { ra.SubjectPrincipalId, ra.RoleId, ra.ResourcePrincipalId }).IsUnique();
    }
}

public sealed class PermissionGrantConfiguration : IEntityTypeConfiguration<PermissionGrant>
{
    public void Configure(EntityTypeBuilder<PermissionGrant> builder)
    {
        builder.ToTable("permission_grants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SubjectPrincipalId).HasColumnName("subject_principal_id").IsRequired();
        builder.Property(x => x.PermissionId).HasColumnName("permission_id").IsRequired();
        builder.Property(x => x.ResourcePrincipalId).HasColumnName("resource_principal_id");
        builder.Property(x => x.GrantedByPrincipalId).HasColumnName("granted_by_principal_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne(x => x.SubjectPrincipal).WithMany().HasForeignKey(x => x.SubjectPrincipalId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ResourcePrincipal).WithMany().HasForeignKey(x => x.ResourcePrincipalId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SubjectPrincipalId, x.PermissionId, x.ResourcePrincipalId }).IsUnique();
    }
}
