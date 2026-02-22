using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasQueryFilter(x => x.DeletedAt == null);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(x => x.Bit).IsUnique();
    }
}
