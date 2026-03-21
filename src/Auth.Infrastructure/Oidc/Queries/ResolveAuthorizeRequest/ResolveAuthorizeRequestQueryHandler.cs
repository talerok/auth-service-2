using System.Collections.Immutable;
using Auth.Application;
using Auth.Application.Oidc.Queries.ResolveAuthorizeRequest;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Infrastructure.Oidc.Queries.ResolveAuthorizeRequest;

internal sealed class ResolveAuthorizeRequestQueryHandler(
    AuthDbContext dbContext,
    IOpenIddictApplicationManager appManager,
    IOpenIddictAuthorizationManager authorizationManager)
    : IRequestHandler<ResolveAuthorizeRequestQuery, AuthorizeRequestResult>
{
    public async Task<AuthorizeRequestResult> Handle(
        ResolveAuthorizeRequestQuery query, CancellationToken cancellationToken)
    {
        var app = await dbContext.Applications
            .FirstOrDefaultAsync(x => x.ClientId == query.ClientId, cancellationToken);

        if (app is null)
            throw new AuthException(AuthErrorCatalog.ApplicationNotFound);
        if (!app.IsActive)
            throw new AuthException(AuthErrorCatalog.ApplicationInactive);

        var application = await appManager.FindByClientIdAsync(query.ClientId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.ApplicationNotFound);

        var applicationId = await appManager.GetIdAsync(application, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.ApplicationNotFound);

        var subject = query.UserId.ToString();
        var scopesArray = query.Scopes.ToImmutableArray();

        // Check for existing valid authorization
        var existingAuth = await FindExistingAuthorizationAsync(
            subject, applicationId, scopesArray, cancellationToken);

        if (existingAuth is not null)
        {
            var existingId = await authorizationManager.GetIdAsync(existingAuth, cancellationToken);
            return new AuthorizeRequestResult(existingId!, ConsentRequired: false);
        }

        var consentType = await appManager.GetConsentTypeAsync(application, cancellationToken);

        if (consentType == ConsentTypes.Explicit)
            return new AuthorizeRequestResult(AuthorizationId: null, ConsentRequired: true);

        // Implicit consent — caller will create authorization via GrantConsentCommand
        return new AuthorizeRequestResult(AuthorizationId: null, ConsentRequired: false);
    }

    private async Task<object?> FindExistingAuthorizationAsync(
        string subject, string applicationId,
        ImmutableArray<string> scopes, CancellationToken cancellationToken)
    {
        await foreach (var auth in authorizationManager.FindAsync(
            subject: subject,
            client: applicationId,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: scopes,
            cancellationToken: cancellationToken))
        {
            return auth;
        }

        return null;
    }
}
