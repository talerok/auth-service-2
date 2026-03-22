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
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var permissionCache = scope.ServiceProvider.GetRequiredService<IPermissionBitCache>();
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
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

        await permissionCache.WarmupAsync(cancellationToken);
        await SeedOidcClientsAsync(appManager, cancellationToken);
    }

    private static async Task SeedNotificationTemplatesAsync(AuthDbContext db, CancellationToken cancellationToken)
    {
        var hasAny = await db.NotificationTemplates.AnyAsync(cancellationToken);
        if (hasAny)
            return;

        db.NotificationTemplates.AddRange(
            new NotificationTemplate
            {
                Channel = TwoFactorChannel.Email,
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
                Channel = TwoFactorChannel.Sms,
                Body = "Your code: {{otp}}"
            });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedApplicationsAsync(AuthDbContext db, CancellationToken cancellationToken)
    {
        var seedClients = new[]
        {
            new
            {
                ClientId = "frontend-app",
                Name = "Frontend SPA",
                RedirectUris = new List<string> { "http://localhost:3000/callback" },
                PostLogoutRedirectUris = new List<string> { "http://localhost:3000" }
            },
            new
            {
                ClientId = "mobile-app",
                Name = "Mobile App",
                RedirectUris = new List<string> { "http://localhost/callback" },
                PostLogoutRedirectUris = new List<string> { "http://localhost" }
            }
        };

        foreach (var seed in seedClients)
        {
            if (!await db.Applications.AnyAsync(x => x.ClientId == seed.ClientId, cancellationToken))
            {
                db.Applications.Add(new Domain.Application
                {
                    ClientId = seed.ClientId,
                    Name = seed.Name,
                    Description = seed.Name,
                    IsActive = true,
                    IsConfidential = false,
                    RedirectUris = seed.RedirectUris,
                    PostLogoutRedirectUris = seed.PostLogoutRedirectUris
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedOidcClientsAsync(IOpenIddictApplicationManager appManager, CancellationToken cancellationToken)
    {
        if (await appManager.FindByClientIdAsync("frontend-app", cancellationToken) is null)
        {
            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "frontend-app",
                DisplayName = "Frontend SPA",
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                RedirectUris = { new Uri("http://localhost:3000/callback") },
                PostLogoutRedirectUris = { new Uri("http://localhost:3000") },
                Permissions =
                {
                    OidcPermissions.Endpoints.Authorization,
                    OidcPermissions.Endpoints.Token,
                    OidcPermissions.Endpoints.EndSession,
                    OidcPermissions.Endpoints.Revocation,
                    OidcPermissions.GrantTypes.AuthorizationCode,
                    OidcPermissions.GrantTypes.Password,
                    OidcPermissions.GrantTypes.RefreshToken,
                    OidcPermissions.ResponseTypes.Code,
                    OidcPermissions.Prefixes.GrantType + OidcConstants.MfaOtpGrantType,
                    OidcPermissions.Prefixes.GrantType + OidcConstants.TokenExchangeGrantType,
                    OidcPermissions.Scopes.Email,
                    OidcPermissions.Scopes.Profile,
                    OidcPermissions.Prefixes.Scope + "ws",
                    OidcPermissions.Prefixes.Scope + "phone"
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            }, cancellationToken);
        }

        if (await appManager.FindByClientIdAsync("mobile-app", cancellationToken) is null)
        {
            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "mobile-app",
                DisplayName = "Mobile App",
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                RedirectUris = { new Uri("http://localhost/callback") },
                PostLogoutRedirectUris = { new Uri("http://localhost") },
                Permissions =
                {
                    OidcPermissions.Endpoints.Authorization,
                    OidcPermissions.Endpoints.Token,
                    OidcPermissions.Endpoints.EndSession,
                    OidcPermissions.Endpoints.Revocation,
                    OidcPermissions.GrantTypes.AuthorizationCode,
                    OidcPermissions.GrantTypes.Password,
                    OidcPermissions.GrantTypes.RefreshToken,
                    OidcPermissions.ResponseTypes.Code,
                    OidcPermissions.Prefixes.GrantType + OidcConstants.MfaOtpGrantType,
                    OidcPermissions.Prefixes.GrantType + OidcConstants.TokenExchangeGrantType,
                    OidcPermissions.Scopes.Email,
                    OidcPermissions.Scopes.Profile,
                    OidcPermissions.Prefixes.Scope + "ws",
                    OidcPermissions.Prefixes.Scope + "phone"
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            }, cancellationToken);
        }
    }
}
