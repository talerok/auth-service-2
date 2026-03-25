using Auth.Domain;
using MediatR;

namespace Auth.Application.TwoFactor.Commands.ConfirmTwoFactorActivation;

public sealed record ConfirmTwoFactorActivationCommand(
    Guid UserId,
    Guid ChallengeId,
    TwoFactorChannel Channel,
    string Otp) : IRequest, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.ConfirmTwoFactorActivation;
    public Guid EntityId => UserId;
    public bool Critical => true;
}
