using MediatR;

namespace Auth.Application.IdentitySources.Queries.GetAllIdentitySources;

public sealed record GetAllIdentitySourcesQuery() : IRequest<IReadOnlyCollection<IdentitySourceDto>>;
