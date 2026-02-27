using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TwoFactorChannel)
            .HasConversion(
                value => value.HasValue ? value.Value.ToString().ToLowerInvariant() : null,
                value => string.IsNullOrWhiteSpace(value) ? null : Enum.Parse<TwoFactorChannel>(value, true))
            .HasMaxLength(16);
        builder.Property(x => x.MustChangePassword).IsRequired().HasDefaultValue(false);
        builder.HasQueryFilter(x => x.DeletedAt == null);
        builder.HasIndex(x => x.Username).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(x => x.Email).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
    }
}
