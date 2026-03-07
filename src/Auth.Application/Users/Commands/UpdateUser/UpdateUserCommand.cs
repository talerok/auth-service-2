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
    TwoFactorChannel? TwoFactorChannel = null) : IRequest<UserDto?>;
