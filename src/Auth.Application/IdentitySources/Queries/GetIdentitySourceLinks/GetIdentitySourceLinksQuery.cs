using MediatR;

namespace Auth.Application.IdentitySources.Queries.GetIdentitySourceLinks;

public sealed record GetIdentitySourceLinksQuery(Guid IdentitySourceId) : IRequest<IReadOnlyCollection<IdentitySourceLinkDto>>;
