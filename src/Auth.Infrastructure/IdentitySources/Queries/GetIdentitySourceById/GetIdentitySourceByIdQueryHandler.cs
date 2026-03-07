using Auth.Application;
using Auth.Application.IdentitySources.Queries.GetIdentitySourceById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.IdentitySources.Queries.GetIdentitySourceById;

internal sealed class GetIdentitySourceByIdQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetIdentitySourceByIdQuery, IdentitySourceDetailDto?>
{
    public async Task<IdentitySourceDetailDto?> Handle(GetIdentitySourceByIdQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.IdentitySources.AsNoTracking()
            .Include(x => x.OidcConfig)
            .Include(x => x.LdapConfig)
            .Where(x => x.Id == query.Id)
            .Select(x => new IdentitySourceDetailDto(
                x.Id, x.Name, x.Code, x.DisplayName, x.Type, x.IsEnabled, x.CreatedAt,
                x.OidcConfig != null
                    ? new IdentitySourceOidcConfigDto(x.OidcConfig.Authority, x.OidcConfig.ClientId, x.OidcConfig.ClientSecret != null)
                    : null,
                x.LdapConfig != null
                    ? new IdentitySourceLdapConfigDto(x.LdapConfig.Host, x.LdapConfig.Port, x.LdapConfig.BaseDn, x.LdapConfig.UseSsl)
                    : null))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
