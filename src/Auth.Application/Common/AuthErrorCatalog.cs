namespace Auth.Application;

public static class AuthErrorCatalog
{
    public const string AuthenticationRequired = "AUTH_AUTHENTICATION_REQUIRED";
    public const string AuthenticationFailed = "AUTH_AUTHENTICATION_FAILED";
    public const string AccessDenied = "AUTH_ACCESS_DENIED";
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string InvalidRefreshToken = "AUTH_INVALID_REFRESH_TOKEN";
    public const string UserInactive = "AUTH_USER_INACTIVE";
    public const string UserNotFound = "AUTH_USER_NOT_FOUND";
    public const string InvalidUserContext = "AUTH_INVALID_USER_CONTEXT";
    public const string DuplicateIdentity = "AUTH_DUPLICATE_IDENTITY";
    public const string DuplicateIdsNotAllowed = "AUTH_DUPLICATE_IDS_NOT_ALLOWED";
    public const string SystemWorkspaceDeleteForbidden = "AUTH_SYSTEM_WORKSPACE_DELETE_FORBIDDEN";
    public const string SystemPermissionDeleteForbidden = "AUTH_SYSTEM_PERMISSION_DELETE_FORBIDDEN";
    public const string PasswordChangeRequired = "AUTH_PASSWORD_CHANGE_REQUIRED";
    public const string InvalidPasswordChangeChallenge = "AUTH_INVALID_PASSWORD_CHANGE_CHALLENGE";
}
