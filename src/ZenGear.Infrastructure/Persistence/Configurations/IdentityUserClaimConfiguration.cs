using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZenGear.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for IdentityUserClaim.
/// </summary>
public class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<long>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<long>> builder)
    {
        builder.ToTable("UserClaims");

        // Primary Key - BIGSERIAL
        builder.HasKey(uc => uc.Id);
        builder.Property(uc => uc.Id)
            .UseIdentityAlwaysColumn();

        // Index on UserId
        builder.HasIndex(uc => uc.UserId)
            .HasDatabaseName("IX_UserClaims_UserId");
    }
}
