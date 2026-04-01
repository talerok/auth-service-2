using System.Security.Claims;
using Auth.Application;
using Auth.Application.Auth.Queries.GetActiveUser;
using Auth.Application.Common;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Oidc.Queries.BuildPrincipal;

internal sealed class BuildPrincipalQueryHandler(
    ISender sender,
    AuthDbContext dbContext,
    IOptions<PasswordExpirationOptions> passwordExpirationOptions) : IRequestHandler<BuildPrincipalQuery, ClaimsPrincipal>
{
    public async Task<ClaimsPrincipal> Handle(BuildPrincipalQuery query, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new GetActiveUserQuery(query.UserId), cancellationToken);
        var scopes = await FilterScopesAsync(query.Scopes, query.ClientId, cancellationToken);
        return await OidcPrincipalFactory.CreateUserPrincipalAsync(
            user, scopes, sender, query.ClientId, cancellationToken, query.AuthMethods,
            passwordExpirationOptions.Value.DefaultMaxAgeDays, query.SessionId);
    }

    private async Task<IReadOnlyCollection<string>> FilterScopesAsync(
        IReadOnlyCollection<string> requestedScopes, string? clientId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return requestedScopes;

        var allowedScopes = await dbContext.Applications
            .AsNoTracking()
            .Where(x => x.ClientId == clientId)
            .Select(x => x.Scopes)
            .FirstOrDefaultAsync(ct);

        if (allowedScopes is null || allowedScopes.Count == 0)
            return requestedScopes;

        return ScopeFilter.Filter(requestedScopes, allowedScopes);
    }
}
