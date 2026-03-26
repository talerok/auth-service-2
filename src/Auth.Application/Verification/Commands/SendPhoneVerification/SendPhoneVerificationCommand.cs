using Auth.Domain;
using MediatR;

namespace Auth.Application.Verification.Commands.SendPhoneVerification;

public sealed record SendPhoneVerificationCommand(Guid UserId) : IRequest<SendVerificationResponse>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.SendPhoneVerification;
    public Guid EntityId => UserId;
    public bool Critical => true;
}
