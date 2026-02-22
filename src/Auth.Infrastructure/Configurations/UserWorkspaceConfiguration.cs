using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class UserWorkspaceConfiguration : IEntityTypeConfiguration<UserWorkspace>
{
    public void Configure(EntityTypeBuilder<UserWorkspace> builder)
    {
        builder.ToTable("user_workspaces");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.User).WithMany(x => x.UserWorkspaces).HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Workspace).WithMany(x => x.UserWorkspaces).HasForeignKey(x => x.WorkspaceId);
        builder.HasIndex(x => new { x.UserId, x.WorkspaceId }).IsUnique();
    }
}
