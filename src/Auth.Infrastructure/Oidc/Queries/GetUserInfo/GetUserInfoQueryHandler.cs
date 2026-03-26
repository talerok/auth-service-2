using Auth.Application;
using Auth.Application.Oidc.Queries.GetUserInfo;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Infrastructure.Oidc.Queries.GetUserInfo;

internal sealed class GetUserInfoQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetUserInfoQuery, Dictionary<string, object>>
{
    public async Task<Dictionary<string, object>> Handle(
        GetUserInfoQuery query, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.UserId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.UserNotFound);

        var scopes = query.Scopes;
        var claims = new Dictionary<string, object>
        {
            [Claims.Subject] = user.Id.ToString()
        };

        if (scopes.Contains(Scopes.Profile))
        {
            claims[Claims.Name] = user.FullName;
            claims[Claims.PreferredUsername] = user.Username;
        }

        if (scopes.Contains(Scopes.Email) && user.Email is not null)
        {
            claims[Claims.Email] = user.Email;
            claims["email_verified"] = true;
        }

        if (scopes.Contains(Scopes.Phone) && !string.IsNullOrWhiteSpace(user.Phone))
        {
            claims[Claims.PhoneNumber] = user.Phone;
        }

        return claims;
    }
}
