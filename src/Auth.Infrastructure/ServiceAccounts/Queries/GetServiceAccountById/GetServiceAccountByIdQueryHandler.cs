using Auth.Application;
using Auth.Application.ServiceAccounts.Queries.GetServiceAccountById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.ServiceAccounts.Queries.GetServiceAccountById;

internal sealed class GetServiceAccountByIdQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetServiceAccountByIdQuery, ServiceAccountDto?>
{
    public async Task<ServiceAccountDto?> Handle(GetServiceAccountByIdQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.ServiceAccounts.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new ServiceAccountDto(x.Id, x.Name, x.Description, x.ClientId, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
