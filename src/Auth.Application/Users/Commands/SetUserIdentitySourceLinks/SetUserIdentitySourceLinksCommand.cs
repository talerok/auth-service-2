using Auth.Domain;
using MediatR;

namespace Auth.Application.Users.Commands.SetUserIdentitySourceLinks;

public sealed record SetUserIdentitySourceLinksCommand(
    Guid UserId,
    IReadOnlyCollection<UserIdentitySourceLinkItem> Links) : IRequest, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.SetIdentitySourceLinks;
    public Guid EntityId => UserId;
}
