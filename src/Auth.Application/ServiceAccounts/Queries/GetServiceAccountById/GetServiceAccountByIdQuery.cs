using MediatR;

namespace Auth.Application.ServiceAccounts.Queries.GetServiceAccountById;

public sealed record GetServiceAccountByIdQuery(Guid Id) : IRequest<ServiceAccountDto?>;
