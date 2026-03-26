using Auth.Domain;
using MediatR;

namespace Auth.Application.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    Guid Id,
    string Username,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive,
    bool IsInternalAuthEnabled = true,
    bool TwoFactorEnabled = false,
    TwoFactorChannel? TwoFactorChannel = null,
    string Locale = "en-US",
    bool EmailVerified = false,
    bool PhoneVerified = false) : IRequest<UserDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.Update;
    public Guid EntityId => Id;
}
