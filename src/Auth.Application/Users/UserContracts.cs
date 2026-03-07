using Auth.Domain;

namespace Auth.Application;

public sealed record UserDto(
    Guid Id,
    string Username,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive,
    bool IsInternalAuthEnabled,
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
    bool IsInternalAuthEnabled = true,
    bool MustChangePassword = false,
    bool TwoFactorEnabled = false,
    TwoFactorChannel? TwoFactorChannel = null);

public sealed record UpdateUserRequest(
    string Username,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive,
    bool IsInternalAuthEnabled = true,
    bool TwoFactorEnabled = false,
    TwoFactorChannel? TwoFactorChannel = null);

public sealed record PatchUserRequest(
    string? Username,
    string? FullName,
    string? Email,
    string? Phone,
    bool? IsActive,
    bool? IsInternalAuthEnabled = null,
    bool? TwoFactorEnabled = null,
    TwoFactorChannel? TwoFactorChannel = null);

public sealed record AdminResetPasswordRequest(string Password);

public sealed record ExportUserWorkspaceDto(string WorkspaceCode, IReadOnlyCollection<string> RoleCodes);
public sealed record ExportUserIdentitySourceDto(string IdentitySourceCode, string ExternalIdentity);
public sealed record ExportUserDto(
    string Username,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive,
    bool IsInternalAuthEnabled,
    bool MustChangePassword,
    bool TwoFactorEnabled,
    TwoFactorChannel? TwoFactorChannel,
    IReadOnlyCollection<ExportUserWorkspaceDto> Workspaces,
    IReadOnlyCollection<ExportUserIdentitySourceDto> IdentitySources);
