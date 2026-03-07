using MediatR;

namespace Auth.Application.Users.Commands.SetUserIdentitySourceLinks;

public sealed record SetUserIdentitySourceLinksCommand(
    Guid UserId,
    IReadOnlyCollection<UserIdentitySourceLinkItem> Links) : IRequest;
