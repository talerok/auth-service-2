using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class ServiceAccountWorkspaceConfiguration : IEntityTypeConfiguration<ServiceAccountWorkspace>
{
    public void Configure(EntityTypeBuilder<ServiceAccountWorkspace> builder)
    {
        builder.ToTable("service_account_workspaces");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.ServiceAccount).WithMany(x => x.ServiceAccountWorkspaces).HasForeignKey(x => x.ServiceAccountId);
        builder.HasOne(x => x.Workspace).WithMany().HasForeignKey(x => x.WorkspaceId);
        builder.HasIndex(x => new { x.ServiceAccountId, x.WorkspaceId }).IsUnique();
    }
}
