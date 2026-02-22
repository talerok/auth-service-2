using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasQueryFilter(x => x.DeletedAt == null);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
    }
}
