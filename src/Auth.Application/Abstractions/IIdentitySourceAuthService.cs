namespace Auth.Application;

public interface IIdentitySourceAuthService
{
    Task<PasswordGrantResult> AuthenticateAsync(
        string identitySourceName, string token, IReadOnlyCollection<string> scopes, CancellationToken cancellationToken);
}
