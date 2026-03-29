using Auth.Application.Common;
using Auth.Domain;
using MediatR;

namespace Auth.Application.Users.Commands.PatchUser;

public sealed record PatchUserCommand(
    Guid Id,
    Optional<string> Username,
    Optional<string> FullName,
    Optional<string> Email,
    Optional<string?> Phone,
    Optional<bool> IsActive,
    Optional<bool> IsInternalAuthEnabled,
    Optional<bool> TwoFactorEnabled,
    Optional<TwoFactorChannel?> TwoFactorChannel,
    Optional<string> Locale,
    Optional<bool> EmailVerified,
    Optional<bool> PhoneVerified,
    Optional<int?> PasswordMaxAgeDays) : IRequest<UserDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.Patch;
    public Guid EntityId => Id;
}
