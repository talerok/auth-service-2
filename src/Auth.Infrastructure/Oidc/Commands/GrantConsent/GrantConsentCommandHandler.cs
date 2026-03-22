using System.Collections.Immutable;
using Auth.Application;
using Auth.Application.Oidc.Commands.GrantConsent;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Infrastructure.Oidc.Commands.GrantConsent;

internal sealed class GrantConsentCommandHandler(
    AuthDbContext dbContext,
    IOpenIddictApplicationManager appManager,
    IOpenIddictAuthorizationManager authorizationManager)
    : IRequestHandler<GrantConsentCommand, string>
{
    public async Task<string> Handle(GrantConsentCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.Applications
            .FirstOrDefaultAsync(x => x.ClientId == command.ClientId, cancellationToken);

        if (app is null)
            throw new AuthException(AuthErrorCatalog.ApplicationNotFound);
        if (!app.IsActive)
            throw new AuthException(AuthErrorCatalog.ApplicationInactive);

        var application = await appManager.FindByClientIdAsync(command.ClientId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.ApplicationNotFound);

        var applicationId = await appManager.GetIdAsync(application, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.ApplicationNotFound);

        var subject = command.UserId.ToString();
        var scopesArray = command.Scopes.ToImmutableArray();

        // Return existing authorization if one already covers the requested scopes
        await foreach (var existing in authorizationManager.FindAsync(
            subject: subject,
            client: applicationId,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: scopesArray,
            cancellationToken: cancellationToken))
        {
            return (await authorizationManager.GetIdAsync(existing, cancellationToken))!;
        }

        var auth = await authorizationManager.CreateAsync(
            identity: null!,
            subject: subject,
            client: applicationId,
            type: AuthorizationTypes.Permanent,
            scopes: scopesArray,
            cancellationToken: cancellationToken);

        return (await authorizationManager.GetIdAsync(auth, cancellationToken))!;
    }
}
