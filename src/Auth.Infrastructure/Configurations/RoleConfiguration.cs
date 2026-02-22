using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasQueryFilter(x => x.DeletedAt == null);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
    }
}
