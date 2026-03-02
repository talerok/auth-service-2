using Auth.Application;
using Auth.Domain;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;

namespace Auth.Infrastructure;

internal interface ILdapAuthenticator
{
    Task<string> AuthenticateAsync(IdentitySourceLdapConfig config, string username, string password, CancellationToken cancellationToken);
}

internal sealed class LdapAuthenticator(ILogger<LdapAuthenticator> logger) : ILdapAuthenticator
{
    public Task<string> AuthenticateAsync(
        IdentitySourceLdapConfig config, string username, string password, CancellationToken cancellationToken)
    {
        using var connection = new LdapConnection();

        if (config.UseSsl)
            connection.SecureSocketLayer = true;

        connection.Connect(config.Host, config.Port);

        if (!string.IsNullOrWhiteSpace(config.BindDn))
            connection.Bind(config.BindDn, config.BindPassword ?? string.Empty);

        var filter = config.SearchFilter.Replace("{username}", EscapeLdapFilter(username));

        var searchResults = connection.Search(
            config.BaseDn,
            LdapConnection.ScopeSub,
            filter,
            null,
            false);

        if (!searchResults.HasMore())
        {
            logger.LogWarning("LDAP user not found for username {Username}", username);
            throw new AuthException(AuthErrorCatalog.IdentitySourceTokenInvalid);
        }

        var entry = searchResults.Next();
        var userDn = entry.Dn;

        using var userConnection = new LdapConnection();
        if (config.UseSsl)
            userConnection.SecureSocketLayer = true;

        userConnection.Connect(config.Host, config.Port);

        try
        {
            userConnection.Bind(userDn, password);
        }
        catch (LdapException ex)
        {
            logger.LogWarning(ex, "LDAP bind failed for user {UserDn}", userDn);
            throw new AuthException(AuthErrorCatalog.IdentitySourceTokenInvalid);
        }

        return Task.FromResult(username);
    }

    private static string EscapeLdapFilter(string input)
    {
        return input
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");
    }
}
