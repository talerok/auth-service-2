using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure;

public static class SeedDataExtensions
{
    public static async Task SeedAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IPermissionBitCache>();
        var jwtOptions = scope.ServiceProvider.GetRequiredService<IOptions<IntegrationOptions>>();
        await db.Database.MigrateAsync(cancellationToken);

        var existingBits = await db.Permissions.IgnoreQueryFilters()
            .Select(x => x.Bit)
            .ToListAsync(cancellationToken);
        var existingBitSet = existingBits.ToHashSet();
        var missingPermissions = SystemPermissionCatalog.Permissions
            .Where(x => !existingBitSet.Contains(x.Bit))
            .ToList();
        if (missingPermissions.Count > 0)
        {
            db.Permissions.AddRange(missingPermissions.Select(x => new Permission
            {
                Bit = x.Bit,
                Code = x.Code,
                Description = x.Description,
                IsSystem = true
            }));
            await db.SaveChangesAsync(cancellationToken);
        }

        var workspace = await db.Workspaces.FirstOrDefaultAsync(x => x.Name == "default", cancellationToken);
        if (workspace is null)
        {
            workspace = new Workspace { Name = "default", Code = "default", Description = "Default system workspace", IsSystem = true };
            db.Workspaces.Add(workspace);
            await db.SaveChangesAsync(cancellationToken);
        }

        var role = await db.Roles.FirstOrDefaultAsync(x => x.Name == "admin", cancellationToken);
        if (role is null)
        {
            role = new Role { Name = "admin", Description = "System administrator" };
            db.Roles.Add(role);
            await db.SaveChangesAsync(cancellationToken);
        }

        var allPermissionIds = await db.Permissions.Select(x => x.Id).ToListAsync(cancellationToken);
        var existingCount = await db.RolePermissions.CountAsync(x => x.RoleId == role.Id, cancellationToken);
        if (existingCount != allPermissionIds.Count)
        {
            db.RolePermissions.RemoveRange(db.RolePermissions.Where(x => x.RoleId == role.Id));
            db.RolePermissions.AddRange(allPermissionIds.Select(id => new RolePermission { RoleId = role.Id, PermissionId = id }));
            await db.SaveChangesAsync(cancellationToken);
        }

        var admin = await db.Users.FirstOrDefaultAsync(x => x.Username == "admin", cancellationToken);
        if (admin is null)
        {
            admin = new User
            {
                Username = "admin",
                Email = "admin@local",
                PasswordHash = hasher.Hash("admin"),
                IsActive = true
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync(cancellationToken);
        }

        var userWorkspace = await db.UserWorkspaces.FirstOrDefaultAsync(
            x => x.UserId == admin.Id && x.WorkspaceId == workspace.Id, cancellationToken);
        if (userWorkspace is null)
        {
            userWorkspace = new UserWorkspace { UserId = admin.Id, WorkspaceId = workspace.Id };
            db.UserWorkspaces.Add(userWorkspace);
            await db.SaveChangesAsync(cancellationToken);
        }

        var hasAdminRole = await db.UserWorkspaceRoles.AnyAsync(
            x => x.UserWorkspaceId == userWorkspace.Id && x.RoleId == role.Id, cancellationToken);
        if (!hasAdminRole)
        {
            db.UserWorkspaceRoles.Add(new UserWorkspaceRole { UserWorkspaceId = userWorkspace.Id, RoleId = role.Id });
            await db.SaveChangesAsync(cancellationToken);
        }

        await permissionCache.WarmupAsync(cancellationToken);
        _ = jwtOptions.Value.Jwt.Secret;
    }
}
