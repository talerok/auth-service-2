using Auth.Application;
using Auth.Application.Applications.Commands.UpdateApplication;
using Auth.Application.Messaging.Commands;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OidcPermissions = OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace Auth.Infrastructure.Applications.Commands.UpdateApplication;

internal sealed class UpdateApplicationCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    ICorsOriginService corsOriginService,
    IOpenIddictApplicationManager appManager,
    IAuditContext auditContext) : IRequestHandler<UpdateApplicationCommand, ApplicationDto?>
{
    public async Task<ApplicationDto?> Handle(UpdateApplicationCommand command, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (application is null)
            return null;

        application.Name = command.Name;
        application.Description = command.Description;
        application.IsActive = command.IsActive;
        application.LogoUrl = command.LogoUrl;
        application.HomepageUrl = command.HomepageUrl;
        application.SetRedirectUris(command.RedirectUris);
        application.SetPostLogoutRedirectUris(command.PostLogoutRedirectUris);
        application.SetAllowedOrigins(command.AllowedOrigins);
        application.SetScopes(command.Scopes);
        application.SetGrantTypes(command.GrantTypes);
        application.SetAudiences(command.Audiences);
        application.AccessTokenLifetimeMinutes = command.AccessTokenLifetimeMinutes;
        application.RefreshTokenLifetimeMinutes = command.RefreshTokenLifetimeMinutes;
        application.RequireEmailVerified = command.RequireEmailVerified;
        application.RequirePhoneVerified = command.RequirePhoneVerified;

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(application));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Application, EntityId = application.Id, Operation = IndexOperation.Index }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        corsOriginService.InvalidateCache();

        await SyncOpenIddictApplication(application, command, cancellationToken);

        var dto = MapToDto(application);
        return dto;
    }

    private async Task SyncOpenIddictApplication(
        Domain.Application application, UpdateApplicationCommand command, CancellationToken cancellationToken)
    {
        var oidcApp = await appManager.FindByClientIdAsync(application.ClientId, cancellationToken);
        if (oidcApp is null)
            return;

        var descriptor = new OpenIddictApplicationDescriptor();
        await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);

        descriptor.DisplayName = command.Name;

        descriptor.Permissions.Clear();
        descriptor.Requirements.Clear();

        GrantTypeMapper.ApplyGrantTypes(descriptor, application.GrantTypes);

        foreach (var scope in application.Scopes)
            descriptor.Permissions.Add(OidcPermissions.Prefixes.Scope + scope);

        descriptor.RedirectUris.Clear();
        foreach (var uri in command.RedirectUris)
            descriptor.RedirectUris.Add(new Uri(uri));

        descriptor.PostLogoutRedirectUris.Clear();
        foreach (var uri in command.PostLogoutRedirectUris)
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri));

        GrantTypeMapper.ApplyTokenLifetimes(descriptor,
            application.AccessTokenLifetimeMinutes, application.RefreshTokenLifetimeMinutes);

        descriptor.ConsentType = command.ConsentType switch
        {
            "implicit" => ConsentTypes.Implicit,
            _ => ConsentTypes.Explicit
        };

        await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
    }

    private static ApplicationDto MapToDto(Domain.Application c) =>
        new(c.Id, c.Name, c.Description, c.ClientId, c.IsActive,
            c.IsConfidential, c.LogoUrl, c.HomepageUrl,
            c.RedirectUris, c.PostLogoutRedirectUris, c.AllowedOrigins, c.Scopes,
            c.GrantTypes, c.Audiences, c.AccessTokenLifetimeMinutes, c.RefreshTokenLifetimeMinutes,
            c.RequireEmailVerified, c.RequirePhoneVerified);
}
