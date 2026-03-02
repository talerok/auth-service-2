using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class IdentitySourceOidcConfigConfiguration : IEntityTypeConfiguration<IdentitySourceOidcConfig>
{
    public void Configure(EntityTypeBuilder<IdentitySourceOidcConfig> builder)
    {
        builder.ToTable("identity_source_oidc_configs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdentitySourceId).IsRequired();
        builder.Property(x => x.Authority).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ClientId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ClientSecret).HasMaxLength(500);
        builder.HasIndex(x => x.IdentitySourceId).IsUnique();
        builder.HasOne<IdentitySource>()
            .WithOne(x => x.OidcConfig)
            .HasForeignKey<IdentitySourceOidcConfig>(x => x.IdentitySourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
