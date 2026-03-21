using Auth.Application;
using Auth.Application.Oidc.Queries.GetClientInfo;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Oidc.Queries.GetClientInfo;

internal sealed class GetClientInfoQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetClientInfoQuery, ClientInfoResult?>
{
    public async Task<ClientInfoResult?> Handle(GetClientInfoQuery query, CancellationToken cancellationToken)
    {
        var client = await dbContext.ApiClients
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == query.ClientId, cancellationToken);

        if (client is null)
            return null;

        return new ClientInfoResult(client.Name, client.LogoUrl, client.HomepageUrl);
    }
}
