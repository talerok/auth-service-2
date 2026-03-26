using Auth.Domain;
using MediatR;

namespace Auth.Application.Users.Commands.PatchUser;

public sealed record PatchUserCommand(
    Guid Id,
    string? Username,
    string? FullName,
    string? Email,
    string? Phone,
    bool? IsActive,
    bool? IsInternalAuthEnabled = null,
    bool? TwoFactorEnabled = null,
    TwoFactorChannel? TwoFactorChannel = null,
    string? Locale = null,
    bool? EmailVerified = null,
    bool? PhoneVerified = null) : IRequest<UserDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.Patch;
    public Guid EntityId => Id;
}
