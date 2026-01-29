using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZenGear.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for IdentityRoleClaim.
/// </summary>
public class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<long>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<long>> builder)
    {
        builder.ToTable("RoleClaims");

        // Primary Key - BIGSERIAL
        builder.HasKey(rc => rc.Id);
        builder.Property(rc => rc.Id)
            .UseIdentityAlwaysColumn();

        // Index on RoleId
        builder.HasIndex(rc => rc.RoleId)
            .HasDatabaseName("IX_RoleClaims_RoleId");
    }
}
