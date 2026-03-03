using Auth.Application;
using Auth.Application.IdentitySources.Queries.GetIdentitySourceLinks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.IdentitySources.Queries.GetIdentitySourceLinks;

internal sealed class GetIdentitySourceLinksQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetIdentitySourceLinksQuery, IReadOnlyCollection<IdentitySourceLinkDto>>
{
    public async Task<IReadOnlyCollection<IdentitySourceLinkDto>> Handle(GetIdentitySourceLinksQuery query, CancellationToken cancellationToken)
    {
        var exists = await dbContext.IdentitySources.AnyAsync(x => x.Id == query.IdentitySourceId, cancellationToken);
        if (!exists)
            throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        return await dbContext.IdentitySourceLinks.AsNoTracking()
            .Where(x => x.IdentitySourceId == query.IdentitySourceId)
            .Select(x => new IdentitySourceLinkDto(x.Id, x.UserId, x.IdentitySourceId, x.ExternalIdentity, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
