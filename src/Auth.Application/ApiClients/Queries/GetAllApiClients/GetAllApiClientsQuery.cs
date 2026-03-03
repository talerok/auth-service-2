using MediatR;

namespace Auth.Application.ApiClients.Queries.GetAllApiClients;

public sealed record GetAllApiClientsQuery() : IRequest<IReadOnlyCollection<ApiClientDto>>;
