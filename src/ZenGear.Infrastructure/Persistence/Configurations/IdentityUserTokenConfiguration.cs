using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ZenGear.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for IdentityUserToken.
/// </summary>
public class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<long>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<long>> builder)
    {
        builder.ToTable("UserTokens");

        // Composite Primary Key
        builder.HasKey(ut => new { ut.UserId, ut.LoginProvider, ut.Name });

        builder.Property(ut => ut.LoginProvider)
            .HasMaxLength(128);

        builder.Property(ut => ut.Name)
            .HasMaxLength(128);
    }
}
