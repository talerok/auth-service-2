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
    bool? TwoFactorEnabled = null,
    TwoFactorChannel? TwoFactorChannel = null) : IRequest<UserDto?>;
