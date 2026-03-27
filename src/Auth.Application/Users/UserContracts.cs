using Auth.Application.Common;
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
    Optional<bool> PhoneVerified);

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
