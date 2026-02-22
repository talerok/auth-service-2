using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class UserWorkspaceRoleConfiguration : IEntityTypeConfiguration<UserWorkspaceRole>
{
    public void Configure(EntityTypeBuilder<UserWorkspaceRole> builder)
    {
        builder.ToTable("user_workspace_roles");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.UserWorkspace).WithMany(x => x.UserWorkspaceRoles).HasForeignKey(x => x.UserWorkspaceId);
        builder.HasOne(x => x.Role).WithMany(x => x.UserWorkspaceRoles).HasForeignKey(x => x.RoleId);
        builder.HasIndex(x => new { x.UserWorkspaceId, x.RoleId }).IsUnique();
    }
}
