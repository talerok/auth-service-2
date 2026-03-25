using Auth.Domain;
using MediatR;

namespace Auth.Application.Auth.Commands.ValidateForcedPasswordChange;

public sealed record ValidateForcedPasswordChangeCommand(Guid ChallengeId, string NewPassword) : IRequest<User>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.PasswordChange;
    public Guid EntityId { get; init; }
    public bool Critical => true;
}
