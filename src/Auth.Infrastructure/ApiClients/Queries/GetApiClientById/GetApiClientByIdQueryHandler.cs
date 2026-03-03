using Auth.Application;
using Auth.Application.ApiClients.Queries.GetApiClientById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.ApiClients.Queries.GetApiClientById;

internal sealed class GetApiClientByIdQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetApiClientByIdQuery, ApiClientDto?>
{
    public async Task<ApiClientDto?> Handle(GetApiClientByIdQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.ApiClients.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new ApiClientDto(x.Id, x.Name, x.Description, x.ClientId, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
