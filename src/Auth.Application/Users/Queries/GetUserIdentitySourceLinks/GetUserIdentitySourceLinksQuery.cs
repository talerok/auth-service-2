using MediatR;

namespace Auth.Application.Users.Queries.GetUserIdentitySourceLinks;

public sealed record GetUserIdentitySourceLinksQuery(Guid UserId) : IRequest<IReadOnlyCollection<UserIdentitySourceLinkDto>?>;
