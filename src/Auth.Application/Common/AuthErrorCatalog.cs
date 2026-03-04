namespace Auth.Application;

public static class AuthErrorCatalog
{
    public const string AuthenticationRequired = "AUTH_AUTHENTICATION_REQUIRED";
    public const string AuthenticationFailed = "AUTH_AUTHENTICATION_FAILED";
    public const string AccessDenied = "AUTH_ACCESS_DENIED";
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string UserInactive = "AUTH_USER_INACTIVE";
    public const string UserNotFound = "AUTH_USER_NOT_FOUND";
    public const string InvalidUserContext = "AUTH_INVALID_USER_CONTEXT";
    public const string DuplicateIdentity = "AUTH_DUPLICATE_IDENTITY";
    public const string DuplicateIdsNotAllowed = "AUTH_DUPLICATE_IDS_NOT_ALLOWED";
    public const string SystemWorkspaceDeleteForbidden = "AUTH_SYSTEM_WORKSPACE_DELETE_FORBIDDEN";
    public const string SystemPermissionDeleteForbidden = "AUTH_SYSTEM_PERMISSION_DELETE_FORBIDDEN";
    public const string SystemPermissionImportForbidden = "AUTH_SYSTEM_PERMISSION_IMPORT_FORBIDDEN";
    public const string PasswordChangeRequired = "AUTH_PASSWORD_CHANGE_REQUIRED";
    public const string InvalidPasswordChangeChallenge = "AUTH_INVALID_PASSWORD_CHANGE_CHALLENGE";
    public const string IdentitySourceNotFound = "AUTH_IDENTITY_SOURCE_NOT_FOUND";
    public const string IdentitySourceDisabled = "AUTH_IDENTITY_SOURCE_DISABLED";
    public const string IdentitySourceTokenInvalid = "AUTH_IDENTITY_SOURCE_TOKEN_INVALID";
    public const string IdentitySourceLinkNotFound = "AUTH_IDENTITY_SOURCE_LINK_NOT_FOUND";
    public const string IdentitySourceUserInactive = "AUTH_IDENTITY_SOURCE_USER_INACTIVE";
    public const string IdentitySourceDuplicateLink = "AUTH_IDENTITY_SOURCE_DUPLICATE_LINK";
    public const string IdentitySourceTypeMismatch = "AUTH_IDENTITY_SOURCE_TYPE_MISMATCH";
    public const string IdentitySourceUsernameRequired = "AUTH_IDENTITY_SOURCE_USERNAME_REQUIRED";
    public const string ApiClientNotFound = "AUTH_API_CLIENT_NOT_FOUND";
    public const string ApiClientInactive = "AUTH_API_CLIENT_INACTIVE";
}
