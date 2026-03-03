using MediatR;

namespace Auth.Application.ApiClients.Queries.GetApiClientById;

public sealed record GetApiClientByIdQuery(Guid Id) : IRequest<ApiClientDto?>;
