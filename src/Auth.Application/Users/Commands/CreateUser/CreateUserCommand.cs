using Auth.Domain;
using MediatR;

namespace Auth.Application.Users.Commands.CreateUser;

public sealed record CreateUserCommand(
    string Username,
    string FullName,
    string Email,
    string Password,
    string? Phone = null,
    bool IsActive = true,
    bool IsInternalAuthEnabled = true,
    bool MustChangePassword = false,
    bool TwoFactorEnabled = false,
    TwoFactorChannel? TwoFactorChannel = null) : IRequest<UserDto>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.Create;
    public Guid EntityId { get; init; } = Guid.NewGuid();
}
