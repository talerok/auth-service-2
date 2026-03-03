using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class ApiClientWorkspaceConfiguration : IEntityTypeConfiguration<ApiClientWorkspace>
{
    public void Configure(EntityTypeBuilder<ApiClientWorkspace> builder)
    {
        builder.ToTable("api_client_workspaces");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.ApiClient).WithMany(x => x.ApiClientWorkspaces).HasForeignKey(x => x.ApiClientId);
        builder.HasOne(x => x.Workspace).WithMany().HasForeignKey(x => x.WorkspaceId);
        builder.HasIndex(x => new { x.ApiClientId, x.WorkspaceId }).IsUnique();
    }
}
