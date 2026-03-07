using Auth.Application;
using Auth.Application.IdentitySources.Queries.GetAllIdentitySources;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.IdentitySources.Queries.GetAllIdentitySources;

internal sealed class GetAllIdentitySourcesQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetAllIdentitySourcesQuery, IReadOnlyCollection<IdentitySourceDto>>
{
    public async Task<IReadOnlyCollection<IdentitySourceDto>> Handle(GetAllIdentitySourcesQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.IdentitySources.AsNoTracking()
            .Select(x => new IdentitySourceDto(x.Id, x.Name, x.Code, x.DisplayName, x.Type, x.IsEnabled, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
