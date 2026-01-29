using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZenGear.Domain.Enums;
using ZenGear.Infrastructure.Identity;

namespace ZenGear.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for EmailOtp entity.
/// Internal entity (NO ExternalId).
/// </summary>
public class EmailOtpConfiguration : IEntityTypeConfiguration<EmailOtp>
{
    public void Configure(EntityTypeBuilder<EmailOtp> builder)
    {
        builder.ToTable("EmailOtps");

        // Primary Key - BIGSERIAL
        builder.HasKey(eo => eo.Id);
        builder.Property(eo => eo.Id)
            .UseIdentityAlwaysColumn();

        // Foreign Key to User
        builder.Property(eo => eo.UserId)
            .IsRequired();

        builder.HasIndex(eo => eo.UserId)
            .HasDatabaseName("IX_EmailOtps_UserId");

        // OTP Code
        builder.Property(eo => eo.Code)
            .IsRequired()
            .HasMaxLength(6)
            .IsFixedLength();

        // Purpose (enum stored as string)
        builder.Property(eo => eo.Purpose)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Composite index for efficient queries (UserId + Purpose)
        builder.HasIndex(eo => new { eo.UserId, eo.Purpose })
            .HasDatabaseName("IX_EmailOtps_UserId_Purpose");

        // Timestamps
        builder.Property(eo => eo.ExpiresAt)
            .IsRequired();

        builder.Property(eo => eo.CreatedAt)
            .IsRequired();

        // IsUsed flag
        builder.Property(eo => eo.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes for queries
        builder.HasIndex(eo => eo.ExpiresAt);
        builder.HasIndex(eo => new { eo.UserId, eo.Purpose, eo.IsUsed });

        // Relationship
        builder.HasOne(eo => eo.User)
            .WithMany(u => u.EmailOtps)
            .HasForeignKey(eo => eo.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
