using Auth.Domain;
using MediatR;

namespace Auth.Application.TwoFactor.Commands.EnableTwoFactor;

public sealed record EnableTwoFactorCommand(
    Guid UserId,
    TwoFactorChannel Channel,
    bool IsHighRisk = false) : IRequest<EnableTwoFactorResponse>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.EnableTwoFactor;
    public Guid EntityId => UserId;
    public bool Critical => true;
}
