using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class IdentitySourceConfiguration : IEntityTypeConfiguration<IdentitySource>
{
    public void Configure(EntityTypeBuilder<IdentitySource> builder)
    {
        builder.ToTable("identity_sources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(16)
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => Enum.Parse<IdentitySourceType>(value, true));
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.HasQueryFilter(x => x.DeletedAt == null);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
    }
}
