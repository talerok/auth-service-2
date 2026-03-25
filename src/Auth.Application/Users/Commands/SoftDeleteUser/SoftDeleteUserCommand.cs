using Auth.Domain;
using MediatR;

namespace Auth.Application.Users.Commands.SoftDeleteUser;

public sealed record SoftDeleteUserCommand(Guid Id) : IRequest<bool>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.SoftDelete;
    public Guid EntityId => Id;
}
