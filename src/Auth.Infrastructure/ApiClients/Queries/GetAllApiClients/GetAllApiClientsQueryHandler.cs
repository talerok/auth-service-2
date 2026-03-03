using Auth.Application;
using Auth.Application.ApiClients.Queries.GetAllApiClients;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.ApiClients.Queries.GetAllApiClients;

internal sealed class GetAllApiClientsQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetAllApiClientsQuery, IReadOnlyCollection<ApiClientDto>>
{
    public async Task<IReadOnlyCollection<ApiClientDto>> Handle(GetAllApiClientsQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.ApiClients.AsNoTracking()
            .Select(x => new ApiClientDto(x.Id, x.Name, x.Description, x.ClientId, x.IsActive))
            .ToListAsync(cancellationToken);
    }
}
