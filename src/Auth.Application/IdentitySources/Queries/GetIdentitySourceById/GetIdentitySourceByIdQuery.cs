using MediatR;

namespace Auth.Application.IdentitySources.Queries.GetIdentitySourceById;

public sealed record GetIdentitySourceByIdQuery(Guid Id) : IRequest<IdentitySourceDetailDto?>;
