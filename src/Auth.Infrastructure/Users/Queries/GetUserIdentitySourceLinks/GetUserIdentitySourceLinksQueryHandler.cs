using Auth.Application;
using Auth.Application.Users.Queries.GetUserIdentitySourceLinks;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Queries.GetUserIdentitySourceLinks;

internal sealed class GetUserIdentitySourceLinksQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetUserIdentitySourceLinksQuery, IReadOnlyCollection<UserIdentitySourceLinkDto>?>
{
    public async Task<IReadOnlyCollection<UserIdentitySourceLinkDto>?> Handle(
        GetUserIdentitySourceLinksQuery query, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AnyAsync(x => x.Id == query.UserId, cancellationToken);
        if (!exists)
            return null;

        return await dbContext.IdentitySourceLinks.AsNoTracking()
            .Where(x => x.UserId == query.UserId)
            .Join(dbContext.IdentitySources, l => l.IdentitySourceId, s => s.Id,
                (l, s) => new UserIdentitySourceLinkDto(
                    l.Id, s.Id, s.Name, s.DisplayName, s.Type, l.ExternalIdentity, l.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
