using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZenGear.Infrastructure.Identity;

namespace ZenGear.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ApplicationUser entity.
/// Configures Identity with BIGSERIAL Id + ExternalId.
/// </summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");

        // Primary Key - BIGSERIAL (auto-increment long)
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .UseIdentityAlwaysColumn();  // PostgreSQL BIGSERIAL

        // ExternalId - Unique index for API lookups
        builder.Property(u => u.ExternalId)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(u => u.ExternalId)
            .IsUnique()
            .HasDatabaseName("IX_Users_ExternalId");

        // Email (already configured by Identity, but we override for PostgreSQL)
        builder.Property(u => u.Email)
            .HasMaxLength(256);

        builder.Property(u => u.NormalizedEmail)
            .HasMaxLength(256);

        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("IX_Users_NormalizedEmail");

        // UserName (required by Identity)
        builder.Property(u => u.UserName)
            .HasMaxLength(256);

        builder.Property(u => u.NormalizedUserName)
            .HasMaxLength(256);

        builder.HasIndex(u => u.NormalizedUserName)
            .IsUnique()
            .HasDatabaseName("IX_Users_NormalizedUserName");

        // Custom fields
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);

        // Status (enum stored as string)
        builder.Property(u => u.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(u => u.Status);

        // Audit fields
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt);

        builder.HasIndex(u => u.CreatedAt);

        // Phone number
        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);

        // Relationships
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.EmailOtps)
            .WithOne(eo => eo.User)
            .HasForeignKey(eo => eo.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
