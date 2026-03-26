using Auth.Domain;
using MediatR;

namespace Auth.Application.Verification.Commands.SendEmailVerification;

public sealed record SendEmailVerificationCommand(Guid UserId) : IRequest<SendVerificationResponse>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.SendEmailVerification;
    public Guid EntityId => UserId;
    public bool Critical => true;
}
