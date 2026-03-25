using Auth.Domain;
using MediatR;

namespace Auth.Application.TwoFactor.Commands.ValidateLoginOtp;

public sealed record ValidateLoginOtpCommand(
    Guid ChallengeId,
    TwoFactorChannel Channel,
    string Otp) : IRequest<User>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.MfaVerify;
    public Guid EntityId { get; init; }
    public bool Critical => true;
}
