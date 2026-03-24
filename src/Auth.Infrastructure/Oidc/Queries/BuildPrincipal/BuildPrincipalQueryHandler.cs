using System.Security.Claims;
using Auth.Application.Auth.Queries.GetActiveUser;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using MediatR;

namespace Auth.Infrastructure.Oidc.Queries.BuildPrincipal;

internal sealed class BuildPrincipalQueryHandler(
    ISender sender) : IRequestHandler<BuildPrincipalQuery, ClaimsPrincipal>
{
    public async Task<ClaimsPrincipal> Handle(BuildPrincipalQuery query, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new GetActiveUserQuery(query.UserId), cancellationToken);
        return await OidcPrincipalFactory.CreateUserPrincipalAsync(
            user, query.Scopes, sender, query.ClientId, cancellationToken);
    }
}
