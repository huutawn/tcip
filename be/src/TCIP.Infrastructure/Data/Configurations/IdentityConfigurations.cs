using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCIP.Business.Modules.Identity.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Enums;

namespace TCIP.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
        builder.Property(u => u.DisplayName).HasColumnName("display_name").IsRequired().HasMaxLength(100);
        builder.Property(u => u.Language).HasColumnName("language").HasMaxLength(16).HasDefaultValue("en").IsRequired();
        builder.Property(u => u.TimeZoneId).HasColumnName("timezone").HasMaxLength(128).HasDefaultValue("UTC").IsRequired();
        builder.Property(u => u.EmailVerified).HasColumnName("email_verified").IsRequired();
        builder.Property(u => u.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(32).HasDefaultValue(UserRole.User).IsRequired();
        builder.Property(u => u.PrincipalId).HasColumnName("principal_id");
        builder.Property(u => u.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(u => u.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne(u => u.Principal)
            .WithOne(p => p.User)
            .HasForeignKey<User>(u => u.PrincipalId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.RefreshTokenHash).HasColumnName("refresh_token_hash").HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(x => x.LastRotatedAtUtc).HasColumnName("last_rotated_at_utc");

        builder.HasIndex(x => x.RefreshTokenHash).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
