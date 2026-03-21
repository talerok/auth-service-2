using Auth.Application;
using Auth.Application.Applications.Commands.PatchApplication;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Infrastructure.Applications.Commands.PatchApplication;

internal sealed class PatchApplicationCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictApplicationManager appManager) : IRequestHandler<PatchApplicationCommand, ApplicationDto?>
{
    public async Task<ApplicationDto?> Handle(PatchApplicationCommand command, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (application is null)
            return null;

        if (command.Name is not null)
            application.Name = command.Name;

        if (command.Description is not null)
            application.Description = command.Description;

        if (command.IsActive.HasValue)
            application.IsActive = command.IsActive.Value;

        if (command.IsConfidential.HasValue)
            application.IsConfidential = command.IsConfidential.Value;

        if (command.LogoUrl is not null)
            application.LogoUrl = command.LogoUrl;

        if (command.HomepageUrl is not null)
            application.HomepageUrl = command.HomepageUrl;

        if (command.RedirectUris is not null)
            application.RedirectUris = command.RedirectUris;

        if (command.PostLogoutRedirectUris is not null)
            application.PostLogoutRedirectUris = command.PostLogoutRedirectUris;

        application.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var needsOidcSync = command.Name is not null
                            || command.IsConfidential.HasValue
                            || command.RedirectUris is not null
                            || command.PostLogoutRedirectUris is not null
                            || command.ConsentType is not null;

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

        if (command.Name is not null)
            descriptor.DisplayName = application.Name;

        if (command.IsConfidential.HasValue)
            descriptor.ClientType = application.IsConfidential ? ClientTypes.Confidential : ClientTypes.Public;

        if (command.RedirectUris is not null)
        {
            descriptor.RedirectUris.Clear();
            foreach (var uri in application.RedirectUris)
                descriptor.RedirectUris.Add(new Uri(uri));
        }

        if (command.PostLogoutRedirectUris is not null)
        {
            descriptor.PostLogoutRedirectUris.Clear();
            foreach (var uri in application.PostLogoutRedirectUris)
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
        }

        if (command.ConsentType is not null)
        {
            descriptor.ConsentType = command.ConsentType switch
            {
                "implicit" => ConsentTypes.Implicit,
                _ => ConsentTypes.Explicit
            };
        }

        await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
    }

    private static ApplicationDto MapToDto(Domain.Application c) =>
        new(c.Id, c.Name, c.Description, c.ClientId, c.IsActive,
            c.IsConfidential, c.LogoUrl, c.HomepageUrl,
            c.RedirectUris, c.PostLogoutRedirectUris);
}
