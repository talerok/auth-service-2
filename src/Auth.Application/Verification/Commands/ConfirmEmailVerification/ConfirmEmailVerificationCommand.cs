using Auth.Domain;
using MediatR;

namespace Auth.Application.Verification.Commands.ConfirmEmailVerification;

public sealed record ConfirmEmailVerificationCommand(Guid ChallengeId, string Otp) : IRequest, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.ConfirmEmailVerification;
    public Guid EntityId { get; init; }
    public bool Critical => true;
}
