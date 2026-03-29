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
    public const string SystemWorkspaceUpdateForbidden = "AUTH_SYSTEM_WORKSPACE_UPDATE_FORBIDDEN";
    public const string SystemPermissionDeleteForbidden = "AUTH_SYSTEM_PERMISSION_DELETE_FORBIDDEN";
    public const string SystemPermissionUpdateForbidden = "AUTH_SYSTEM_PERMISSION_UPDATE_FORBIDDEN";
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
    public const string ApplicationNotFound = "AUTH_APPLICATION_NOT_FOUND";
    public const string ApplicationInactive = "AUTH_APPLICATION_INACTIVE";
    public const string PermissionCodeNotFound = "AUTH_PERMISSION_CODE_NOT_FOUND";
    public const string SystemWorkspaceImportForbidden = "AUTH_SYSTEM_WORKSPACE_IMPORT_FORBIDDEN";
    public const string InternalAuthDisabled = "AUTH_INTERNAL_AUTH_DISABLED";
    public const string ConsentRequired = "AUTH_CONSENT_REQUIRED";
    public const string AuthorizationNotFound = "AUTH_AUTHORIZATION_NOT_FOUND";
    public const string InvalidRedirectUri = "AUTH_INVALID_REDIRECT_URI";
    public const string InvalidScope = "AUTH_INVALID_SCOPE";
    public const string InvalidRequest = "AUTH_INVALID_REQUEST";

    public const string ImportUserInvalidUsername = "IMPORT_USER_INVALID_USERNAME";
    public const string ImportUserInvalidFullName = "IMPORT_USER_INVALID_FULL_NAME";
    public const string ImportUserInvalidEmail = "IMPORT_USER_INVALID_EMAIL";
    public const string ImportUserDuplicateUsername = "IMPORT_USER_DUPLICATE_USERNAME";
    public const string ImportUserDuplicateEmail = "IMPORT_USER_DUPLICATE_EMAIL";
    public const string ImportUserEmailConflict = "IMPORT_USER_EMAIL_CONFLICT";
    public const string ImportUserWorkspaceNotFound = "IMPORT_USER_WORKSPACE_NOT_FOUND";
    public const string ImportUserRoleNotFound = "IMPORT_USER_ROLE_NOT_FOUND";
    public const string ImportUserIdentitySourceNotFound = "IMPORT_USER_IDENTITY_SOURCE_NOT_FOUND";
    public const string ImportUserIdentitySourceLinkConflict = "IMPORT_USER_IDENTITY_SOURCE_LINK_CONFLICT";
    public const string ImportUserInvalidPasswordMaxAgeDays = "IMPORT_USER_INVALID_PASSWORD_MAX_AGE_DAYS";
}
