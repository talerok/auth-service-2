using Auth.Application;
using Auth.Application.Applications.Commands.PatchApplication;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OidcPermissions = OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace Auth.Infrastructure.Applications.Commands.PatchApplication;

internal sealed class PatchApplicationCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    ICorsOriginService corsOriginService,
    IOpenIddictApplicationManager appManager,
    IAuditContext auditContext) : IRequestHandler<PatchApplicationCommand, ApplicationDto?>
{
    public async Task<ApplicationDto?> Handle(PatchApplicationCommand command, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (application is null)
            return null;

        if (command.Name.HasValue)
            application.Name = command.Name.Value!;

        if (command.Description.HasValue)
            application.Description = command.Description.Value!;

        if (command.IsActive.HasValue)
            application.IsActive = command.IsActive.Value;

        if (command.LogoUrl.HasValue)
            application.LogoUrl = command.LogoUrl.Value;

        if (command.HomepageUrl.HasValue)
            application.HomepageUrl = command.HomepageUrl.Value;

        if (command.RedirectUris.HasValue)
            application.SetRedirectUris(command.RedirectUris.Value!);

        if (command.PostLogoutRedirectUris.HasValue)
            application.SetPostLogoutRedirectUris(command.PostLogoutRedirectUris.Value!);

        if (command.AllowedOrigins.HasValue)
            application.SetAllowedOrigins(command.AllowedOrigins.Value!);

        if (command.Scopes.HasValue)
            application.SetScopes(command.Scopes.Value!);

        if (command.GrantTypes.HasValue)
            application.SetGrantTypes(command.GrantTypes.Value!);

        if (command.Audiences.HasValue)
            application.SetAudiences(command.Audiences.Value!);

        if (command.AccessTokenLifetimeMinutes.HasValue)
            application.AccessTokenLifetimeMinutes =
                command.AccessTokenLifetimeMinutes.Value == 0 ? null : command.AccessTokenLifetimeMinutes.Value;

        if (command.RefreshTokenLifetimeMinutes.HasValue)
            application.RefreshTokenLifetimeMinutes =
                command.RefreshTokenLifetimeMinutes.Value == 0 ? null : command.RefreshTokenLifetimeMinutes.Value;

        if (command.RequireEmailVerified.HasValue)
            application.RequireEmailVerified = command.RequireEmailVerified.Value;

        if (command.RequirePhoneVerified.HasValue)
            application.RequirePhoneVerified = command.RequirePhoneVerified.Value;

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(application));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await dbContext.SaveChangesAsync(cancellationToken);
        corsOriginService.InvalidateCache();

        // Audiences are not synced to OpenIddict — stored only in our DB
        // and applied at token issuance time via OidcPrincipalFactory.
        var needsOidcSync = command.Name.HasValue
                            || command.RedirectUris.HasValue
                            || command.PostLogoutRedirectUris.HasValue
                            || command.ConsentType.HasValue
                            || command.Scopes.HasValue
                            || command.GrantTypes.HasValue
                            || command.AccessTokenLifetimeMinutes.HasValue
                            || command.RefreshTokenLifetimeMinutes.HasValue;

        if (needsOidcSync)
            await SyncOpenIddictApplication(application, command, cancellationToken);

        var dto = MapToDto(application);
        await searchIndexService.IndexApplicationAsync(dto, cancellationToken);
        return dto;
    }

    private async Task SyncOpenIddictApplication(
        Domain.Application application, PatchApplicationCommand command, CancellationToken cancellationToken)
    {
        var oidcApp = await appManager.FindByClientIdAsync(application.ClientId, cancellationToken);
        if (oidcApp is null)
            return;

        var descriptor = new OpenIddictApplicationDescriptor();
        await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);

        if (command.Name.HasValue)
            descriptor.DisplayName = application.Name;

        if (command.RedirectUris.HasValue)
        {
            descriptor.RedirectUris.Clear();
            foreach (var uri in application.RedirectUris)
                descriptor.RedirectUris.Add(new Uri(uri));
        }

        if (command.PostLogoutRedirectUris.HasValue)
        {
            descriptor.PostLogoutRedirectUris.Clear();
            foreach (var uri in application.PostLogoutRedirectUris)
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
        }

        if (command.ConsentType.HasValue)
        {
            descriptor.ConsentType = command.ConsentType.Value switch
            {
                "implicit" => ConsentTypes.Implicit,
                _ => ConsentTypes.Explicit
            };
        }

        if (command.GrantTypes.HasValue)
        {
            descriptor.Permissions.RemoveWhere(p =>
                p.StartsWith(OidcPermissions.Prefixes.GrantType)
                || p.StartsWith(OidcPermissions.Prefixes.Endpoint)
                || p.StartsWith(OidcPermissions.Prefixes.ResponseType));
            descriptor.Requirements.Clear();

            GrantTypeMapper.ApplyGrantTypes(descriptor, application.GrantTypes);
        }

        if (command.Scopes.HasValue)
        {
            var scopePrefix = OidcPermissions.Prefixes.Scope;
            descriptor.Permissions.RemoveWhere(p => p.StartsWith(scopePrefix));
            foreach (var scope in application.Scopes)
                descriptor.Permissions.Add(scopePrefix + scope);
        }

        if (command.AccessTokenLifetimeMinutes.HasValue || command.RefreshTokenLifetimeMinutes.HasValue)
        {
            GrantTypeMapper.ApplyTokenLifetimes(descriptor,
                application.AccessTokenLifetimeMinutes, application.RefreshTokenLifetimeMinutes);
        }

        await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
    }

    private static ApplicationDto MapToDto(Domain.Application c) =>
        new(c.Id, c.Name, c.Description, c.ClientId, c.IsActive,
            c.IsConfidential, c.LogoUrl, c.HomepageUrl,
            c.RedirectUris, c.PostLogoutRedirectUris, c.AllowedOrigins, c.Scopes,
            c.GrantTypes, c.Audiences, c.AccessTokenLifetimeMinutes, c.RefreshTokenLifetimeMinutes,
            c.RequireEmailVerified, c.RequirePhoneVerified);
}
