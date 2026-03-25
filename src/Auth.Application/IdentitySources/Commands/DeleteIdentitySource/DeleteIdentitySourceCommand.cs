using Auth.Domain;
using MediatR;

namespace Auth.Application.IdentitySources.Commands.DeleteIdentitySource;

public sealed record DeleteIdentitySourceCommand(Guid Id) : IRequest, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.IdentitySource;
    public AuditAction Action => AuditAction.SoftDelete;
    public Guid EntityId => Id;
}
