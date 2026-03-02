using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class IdentitySourceLdapConfigConfiguration : IEntityTypeConfiguration<IdentitySourceLdapConfig>
{
    public void Configure(EntityTypeBuilder<IdentitySourceLdapConfig> builder)
    {
        builder.ToTable("identity_source_ldap_configs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdentitySourceId).IsRequired();
        builder.Property(x => x.Host).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Port).IsRequired();
        builder.Property(x => x.BaseDn).IsRequired().HasMaxLength(500);
        builder.Property(x => x.BindDn).IsRequired().HasMaxLength(500);
        builder.Property(x => x.BindPassword).HasMaxLength(500);
        builder.Property(x => x.UseSsl).IsRequired();
        builder.Property(x => x.SearchFilter).IsRequired().HasMaxLength(500);
        builder.HasIndex(x => x.IdentitySourceId).IsUnique();
        builder.HasOne<IdentitySource>()
            .WithOne(x => x.LdapConfig)
            .HasForeignKey<IdentitySourceLdapConfig>(x => x.IdentitySourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
