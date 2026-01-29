using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZenGear.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for IdentityRole entity.
/// Uses BIGSERIAL for Id.
/// </summary>
public class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole<long>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<long>> builder)
    {
        builder.ToTable("Roles");

        // Primary Key - BIGSERIAL
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .UseIdentityAlwaysColumn();

        // Name
        builder.Property(r => r.Name)
            .HasMaxLength(100);

        builder.Property(r => r.NormalizedName)
            .HasMaxLength(100);

        builder.HasIndex(r => r.NormalizedName)
            .IsUnique()
            .HasDatabaseName("IX_Roles_NormalizedName");
    }
}
