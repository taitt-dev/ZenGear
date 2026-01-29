using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZenGear.Infrastructure.Identity;

namespace ZenGear.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for RefreshToken entity.
/// Internal entity (NO ExternalId).
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        // Primary Key - BIGSERIAL
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id)
            .UseIdentityAlwaysColumn();

        // Token - Unique index for lookups
        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        // Foreign Key to User
        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        // Timestamps
        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        builder.Property(rt => rt.RevokedAt);

        // Replaced token tracking
        builder.Property(rt => rt.ReplacedByToken)
            .HasMaxLength(100);

        // Indexes for queries
        builder.HasIndex(rt => rt.ExpiresAt);
        builder.HasIndex(rt => rt.RevokedAt);

        // Relationship
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
