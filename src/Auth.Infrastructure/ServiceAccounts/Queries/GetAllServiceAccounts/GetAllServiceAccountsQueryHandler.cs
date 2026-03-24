using Auth.Application;
using Auth.Application.ServiceAccounts.Queries.GetAllServiceAccounts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.ServiceAccounts.Queries.GetAllServiceAccounts;

internal sealed class GetAllServiceAccountsQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetAllServiceAccountsQuery, IReadOnlyCollection<ServiceAccountDto>>
{
    public async Task<IReadOnlyCollection<ServiceAccountDto>> Handle(GetAllServiceAccountsQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.ServiceAccounts.AsNoTracking()
            .Select(x => new ServiceAccountDto(x.Id, x.Name, x.Description, x.ClientId, x.IsActive, x.Audiences, x.AccessTokenLifetimeMinutes))
            .ToListAsync(cancellationToken);
    }
}
