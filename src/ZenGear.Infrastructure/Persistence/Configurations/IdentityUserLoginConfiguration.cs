using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZenGear.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for IdentityUserLogin (for social logins like Google).
/// </summary>
public class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<long>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<long>> builder)
    {
        builder.ToTable("UserLogins");

        // Composite Primary Key
        builder.HasKey(ul => new { ul.LoginProvider, ul.ProviderKey });

        // Properties
        builder.Property(ul => ul.LoginProvider)
            .HasMaxLength(128);

        builder.Property(ul => ul.ProviderKey)
            .HasMaxLength(128);

        builder.Property(ul => ul.ProviderDisplayName)
            .HasMaxLength(256);

        // Index on UserId for lookups
        builder.HasIndex(ul => ul.UserId)
            .HasDatabaseName("IX_UserLogins_UserId");
    }
}
