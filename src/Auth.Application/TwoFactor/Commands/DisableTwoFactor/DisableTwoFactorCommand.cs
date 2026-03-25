using Auth.Domain;
using MediatR;

namespace Auth.Application.TwoFactor.Commands.DisableTwoFactor;

public sealed record DisableTwoFactorCommand(Guid UserId) : IRequest, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.DisableTwoFactor;
    public Guid EntityId => UserId;
    public bool Critical => true;
}
