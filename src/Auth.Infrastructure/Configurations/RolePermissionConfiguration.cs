using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId);
        builder.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId);
        builder.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
    }
}
