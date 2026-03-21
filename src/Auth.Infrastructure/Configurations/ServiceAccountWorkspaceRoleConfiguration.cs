using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class ServiceAccountWorkspaceRoleConfiguration : IEntityTypeConfiguration<ServiceAccountWorkspaceRole>
{
    public void Configure(EntityTypeBuilder<ServiceAccountWorkspaceRole> builder)
    {
        builder.ToTable("service_account_workspace_roles");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.ServiceAccountWorkspace).WithMany(x => x.ServiceAccountWorkspaceRoles).HasForeignKey(x => x.ServiceAccountWorkspaceId);
        builder.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
        builder.HasIndex(x => new { x.ServiceAccountWorkspaceId, x.RoleId }).IsUnique();
    }
}
