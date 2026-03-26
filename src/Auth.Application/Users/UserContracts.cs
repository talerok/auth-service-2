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
    TwoFactorChannel? TwoFactorChannel,
    string Locale,
    bool EmailVerified,
    bool PhoneVerified);

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
    TwoFactorChannel? TwoFactorChannel = null,
    string Locale = "en-US",
    bool EmailVerified = false,
    bool PhoneVerified = false);

public sealed record UpdateUserRequest(
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
    bool PhoneVerified = false);

public sealed record PatchUserRequest(
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
    bool? PhoneVerified = null);

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
    string Locale,
    bool EmailVerified,
    bool PhoneVerified,
    IReadOnlyCollection<ExportUserWorkspaceDto> Workspaces,
    IReadOnlyCollection<ExportUserIdentitySourceDto> IdentitySources);

public sealed record ImportUserItem(
    string Username,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive,
    bool IsInternalAuthEnabled,
    bool MustChangePassword,
    bool TwoFactorEnabled,
    TwoFactorChannel? TwoFactorChannel,
    string Locale = "en-US",
    bool EmailVerified = false,
    bool PhoneVerified = false,
    IReadOnlyCollection<ImportUserWorkspaceItem>? Workspaces = null,
    IReadOnlyCollection<ImportUserIdentitySourceItem>? IdentitySources = null);
public sealed record ImportUserWorkspaceItem(string WorkspaceCode, IReadOnlyCollection<string> RoleCodes);
public sealed record ImportUserIdentitySourceItem(string IdentitySourceCode, string ExternalIdentity);
public sealed record ImportUsersResult(IReadOnlyCollection<ImportUserResultItem> Items, int Blocked);
public sealed record ImportUserResultItem(string Username, string? TemporaryPassword, string Status, string? Error);
