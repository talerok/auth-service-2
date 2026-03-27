using Auth.Domain;
using MediatR;

namespace Auth.Application.Verification.Commands.ConfirmPhoneVerification;

public sealed record ConfirmPhoneVerificationCommand(Guid ChallengeId, string Otp) : IRequest, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.ConfirmPhoneVerification;
    public Guid EntityId { get; init; }
    public bool Critical => true;
}
