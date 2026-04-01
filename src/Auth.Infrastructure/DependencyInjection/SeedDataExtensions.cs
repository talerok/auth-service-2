using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OidcPermissions = OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace Auth.Infrastructure;

public static class SeedDataExtensions
{
    public static async Task SeedAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var distributedLock = serviceProvider.GetRequiredService<IDistributedLock>();
        await using var handle = await distributedLock.AcquireAsync("seed-data", cancellationToken);

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IPermissionBitCache>();
        var corsOriginService = scope.ServiceProvider.GetRequiredService<ICorsOriginService>();
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        await db.Database.MigrateAsync(cancellationToken);

        var existingKeys = await db.Permissions.IgnoreQueryFilters()
            .Select(x => new { x.Domain, x.Bit })
            .ToListAsync(cancellationToken);
        var existingKeySet = existingKeys.Select(x => (x.Domain, x.Bit)).ToHashSet();
        var missingPermissions = SystemPermissionCatalog.Permissions
            .Where(x => !existingKeySet.Contains((x.Domain, x.Bit)))
            .ToList();
        if (missingPermissions.Count > 0)
        {
            db.Permissions.AddRange(missingPermissions.Select(x => new Permission
            {
                Domain = x.Domain,
                Bit = x.Bit,
                Code = x.Code,
                Description = x.Description,
                IsSystem = true
            }));
            await db.SaveChangesAsync(cancellationToken);
        }

        var workspace = await db.Workspaces.FirstOrDefaultAsync(x => x.Name == "system", cancellationToken);
        if (workspace is null)
        {
            workspace = new Workspace { Name = "system", Code = "system", Description = "System workspace", IsSystem = true };
            db.Workspaces.Add(workspace);
            await db.SaveChangesAsync(cancellationToken);
        }

        var role = await db.Roles.FirstOrDefaultAsync(x => x.Name == "admin", cancellationToken);
        if (role is null)
        {
            role = new Role { Name = "admin", Code = "admin", Description = "System administrator" };
            db.Roles.Add(role);
            await db.SaveChangesAsync(cancellationToken);
        }

        var systemPermissionIds = await db.Permissions
            .Where(x => x.IsSystem)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var existingRolePermissionIds = await db.RolePermissions
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);
        if (!existingRolePermissionIds.ToHashSet().SetEquals(systemPermissionIds))
        {
            db.RolePermissions.RemoveRange(db.RolePermissions.Where(x => x.RoleId == role.Id));
            db.RolePermissions.AddRange(systemPermissionIds.Select(id => new RolePermission { RoleId = role.Id, PermissionId = id }));
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
                IsActive = true,
                IsInternalAuthEnabled = true
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

        await SeedNotificationTemplatesAsync(db, cancellationToken);
        await SeedApplicationsAsync(db, cancellationToken);
        await SeedWorkspaceScopesAsync(scopeManager, db, cancellationToken);

        await permissionCache.WarmupAsync(cancellationToken);
        await corsOriginService.WarmupAsync(cancellationToken);
        await SeedOidcClientsAsync(appManager, cancellationToken);
    }

    private static async Task SeedNotificationTemplatesAsync(AuthDbContext db, CancellationToken cancellationToken)
    {
        var existingTypes = await db.NotificationTemplates
            .Where(x => x.Locale == "en-US")
            .Select(x => x.Type)
            .ToListAsync(cancellationToken);
        var existingTypeSet = existingTypes.ToHashSet();

        var templates = GetDefaultTemplates()
            .Where(t => !existingTypeSet.Contains(t.Type))
            .ToList();

        if (templates.Count == 0)
            return;

        db.NotificationTemplates.AddRange(templates);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<NotificationTemplate> GetDefaultTemplates() =>
    [
        new NotificationTemplate
        {
            Type = NotificationTemplateType.TwoFactorEmail,
            Locale = "en-US",
            Subject = "Your verification code",
            Body = """
                <html>
                <body style="font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;">
                  <div style="max-width: 480px; margin: 0 auto; background: #fff; border-radius: 8px; padding: 32px;">
                    <h2 style="color: #333;">Your verification code</h2>
                    <p style="color: #555;">Use the code below to complete your sign-in.</p>
                    <div style="font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #111; margin: 24px 0;">{{otp}}</div>
                    <p style="color: #888; font-size: 13px;">This code expires in a few minutes. Do not share it with anyone.</p>
                    <p style="color: #ccc; font-size: 11px;">Reference: {{email}}</p>
                  </div>
                </body>
                </html>
                """
        },
        new NotificationTemplate
        {
            Type = NotificationTemplateType.TwoFactorSms,
            Locale = "en-US",
            Body = "Your code: {{otp}}"
        },
        new NotificationTemplate
        {
            Type = NotificationTemplateType.EmailVerification,
            Locale = "en-US",
            Subject = "Verify your email address",
            Body = """
                <html>
                <body style="font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;">
                  <div style="max-width: 480px; margin: 0 auto; background: #fff; border-radius: 8px; padding: 32px;">
                    <h2 style="color: #333;">Verify your email</h2>
                    <p style="color: #555;">Click the link below or use the code to verify your email address.</p>
                    <div style="font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #111; margin: 24px 0;">{{otp}}</div>
                    <p><a href="{{link}}" style="color: #1a73e8;">Verify email</a></p>
                    <p style="color: #888; font-size: 13px;">This code expires in a few minutes. Do not share it with anyone.</p>
                  </div>
                </body>
                </html>
                """
        },
        new NotificationTemplate
        {
            Type = NotificationTemplateType.PhoneVerification,
            Locale = "en-US",
            Body = "Your verification code: {{otp}}"
        }
    ];

    private static async Task SeedApplicationsAsync(AuthDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Applications.AnyAsync(x => x.ClientId == "system-app", cancellationToken))
        {
            var app = new Domain.Application
            {
                ClientId = "system-app",
                Name = "System Application",
                Description = "System Application",
                IsActive = true,
                IsConfidential = false,
            };
            app.SetScopes(["openid", "profile", "email", "ws:system", "ws:*", "offline_access"]);
            app.SetGrantTypes(["client_credentials", "jwt-bearer", "ldap", "password", "mfa_otp", "refresh_token"]);
            app.SetAllowedOrigins(["http://localhost:4200"]);
            db.Applications.Add(app);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedWorkspaceScopesAsync(
        IOpenIddictScopeManager scopeManager, AuthDbContext db, CancellationToken cancellationToken)
    {
        var workspaceCodes = await db.Workspaces
            .Select(w => w.Code)
            .ToListAsync(cancellationToken);

        foreach (var code in workspaceCodes)
        {
            var scopeName = $"ws:{code}";
            if (await scopeManager.FindByNameAsync(scopeName, cancellationToken) is null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = scopeName,
                    DisplayName = $"Workspace: {code}"
                }, cancellationToken);
            }
        }
    }

    private static async Task SeedOidcClientsAsync(IOpenIddictApplicationManager appManager, CancellationToken cancellationToken)
    {
        if (await appManager.FindByClientIdAsync("system-app", cancellationToken) is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "system-app",
                DisplayName = "System Application",
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit
            };

            Applications.GrantTypeMapper.ApplyGrantTypes(descriptor,
                ["client_credentials", "jwt-bearer", "ldap", "password", "mfa_otp", "refresh_token"]);

            foreach (var scope in (string[])["openid", "profile", "email", "ws:system", "ws:*", "offline_access"])
                descriptor.Permissions.Add(OidcPermissions.Prefixes.Scope + scope);

            await appManager.CreateAsync(descriptor, cancellationToken);
        }
    }
}
