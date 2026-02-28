using Auth.Domain;

namespace Auth.Application;

public sealed record UserDto(
    Guid Id,
    string Username,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive,
    bool MustChangePassword,
    bool TwoFactorEnabled,
    TwoFactorChannel? TwoFactorChannel);

public sealed record CreateUserRequest(
    string Username,
    string FullName,
    string Email,
    string Password,
    string? Phone = null,
    bool IsActive = true,
    bool MustChangePassword = false,
    bool TwoFactorEnabled = false,
    TwoFactorChannel? TwoFactorChannel = null);

public sealed record UpdateUserRequest(
    string Username,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive,
    bool MustChangePassword = false,
    bool TwoFactorEnabled = false,
    TwoFactorChannel? TwoFactorChannel = null);

public sealed record PatchUserRequest(
    string? Username,
    string? FullName,
    string? Email,
    string? Phone,
    bool? IsActive,
    bool? MustChangePassword = null,
    bool? TwoFactorEnabled = null,
    TwoFactorChannel? TwoFactorChannel = null);
