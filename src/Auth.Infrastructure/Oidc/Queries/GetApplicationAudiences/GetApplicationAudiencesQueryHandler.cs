using Auth.Application.Oidc.Queries.GetApplicationAudiences;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Oidc.Queries.GetApplicationAudiences;

internal sealed class GetApplicationAudiencesQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetApplicationAudiencesQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(
        GetApplicationAudiencesQuery query, CancellationToken cancellationToken)
    {
        var audiences = await dbContext.Applications.AsNoTracking()
            .Where(x => x.ClientId == query.ClientId)
            .Select(x => x.Audiences)
            .FirstOrDefaultAsync(cancellationToken);

        return audiences ?? [];
    }
}
