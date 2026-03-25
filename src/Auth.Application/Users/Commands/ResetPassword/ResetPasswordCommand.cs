using Auth.Domain;
using MediatR;

namespace Auth.Application.Users.Commands.ResetPassword;

public sealed record ResetPasswordCommand(Guid UserId, string NewPassword) : IRequest<bool>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.ResetPassword;
    public Guid EntityId => UserId;
}
