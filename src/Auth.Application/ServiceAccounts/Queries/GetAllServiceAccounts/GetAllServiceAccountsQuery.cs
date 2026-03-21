using MediatR;

namespace Auth.Application.ServiceAccounts.Queries.GetAllServiceAccounts;

public sealed record GetAllServiceAccountsQuery() : IRequest<IReadOnlyCollection<ServiceAccountDto>>;
