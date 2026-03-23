using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class ApplicationConfiguration : IEntityTypeConfiguration<Domain.Application>
{
    public void Configure(EntityTypeBuilder<Domain.Application> builder)
    {
        builder.ToTable("applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.ClientId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.IsConfidential).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.LogoUrl).HasMaxLength(2000);
        builder.Property(x => x.HomepageUrl).HasMaxLength(2000);

        builder.Property(x => x.RedirectUris)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.Property(x => x.PostLogoutRedirectUris)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.Property(x => x.AllowedOrigins)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.Property(x => x.Scopes)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.Property(x => x.GrantTypes)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[\"authorization_code\", \"refresh_token\"]'::jsonb");

        builder.Property(x => x.AccessTokenLifetimeMinutes);
        builder.Property(x => x.RefreshTokenLifetimeMinutes);

        builder.HasQueryFilter(x => x.DeletedAt == null);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(x => x.ClientId).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
    }
}
