using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class ApiClientWorkspaceRoleConfiguration : IEntityTypeConfiguration<ApiClientWorkspaceRole>
{
    public void Configure(EntityTypeBuilder<ApiClientWorkspaceRole> builder)
    {
        builder.ToTable("api_client_workspace_roles");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.ApiClientWorkspace).WithMany(x => x.ApiClientWorkspaceRoles).HasForeignKey(x => x.ApiClientWorkspaceId);
        builder.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
        builder.HasIndex(x => new { x.ApiClientWorkspaceId, x.RoleId }).IsUnique();
    }
}
