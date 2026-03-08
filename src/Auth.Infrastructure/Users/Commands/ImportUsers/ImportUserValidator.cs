using System.Text.RegularExpressions;
using Auth.Application;
using Auth.Domain;

namespace Auth.Infrastructure.Users.Commands.ImportUsers;

internal sealed class ImportUserValidator(
    Dictionary<string, User> existingUsers,
    HashSet<string> existingEmailSet,
    Dictionary<string, Workspace> workspacesByCode,
    Dictionary<string, Role> rolesByCode,
    Dictionary<string, IdentitySource> identitySourcesByCode,
    HashSet<(Guid IdentitySourceId, string ExternalIdentity)> takenIdentityLinks)
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private readonly HashSet<string> _seenUsernames = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _seenEmails = new(StringComparer.OrdinalIgnoreCase);

    public string? Validate(ImportUserItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Username) || item.Username.Length > 100)
            return AuthErrorCatalog.ImportUserInvalidUsername;

        if (string.IsNullOrWhiteSpace(item.FullName) || item.FullName.Length > 200)
            return AuthErrorCatalog.ImportUserInvalidFullName;

        if (string.IsNullOrWhiteSpace(item.Email) || !EmailRegex.IsMatch(item.Email))
            return AuthErrorCatalog.ImportUserInvalidEmail;

        if (!_seenUsernames.Add(item.Username))
            return AuthErrorCatalog.ImportUserDuplicateUsername;

        if (!_seenEmails.Add(item.Email))
            return AuthErrorCatalog.ImportUserDuplicateEmail;

        var isNewUser = !existingUsers.ContainsKey(item.Username);
        var emailTakenByOtherImportedUser = existingUsers.Values.Any(u =>
            u.Email.Equals(item.Email, StringComparison.OrdinalIgnoreCase) &&
            !u.Username.Equals(item.Username, StringComparison.OrdinalIgnoreCase));

        if (emailTakenByOtherImportedUser || (isNewUser && existingEmailSet.Contains(item.Email)))
            return AuthErrorCatalog.ImportUserEmailConflict;

        if (item.Workspaces is not null)
        {
            foreach (var ws in item.Workspaces)
            {
                if (!workspacesByCode.ContainsKey(ws.WorkspaceCode))
                    return AuthErrorCatalog.ImportUserWorkspaceNotFound;

                foreach (var roleCode in ws.RoleCodes)
                {
                    if (!rolesByCode.ContainsKey(roleCode))
                        return AuthErrorCatalog.ImportUserRoleNotFound;
                }
            }
        }

        if (item.IdentitySources is not null)
        {
            foreach (var src in item.IdentitySources)
            {
                if (!identitySourcesByCode.TryGetValue(src.IdentitySourceCode, out var idSource))
                    return AuthErrorCatalog.ImportUserIdentitySourceNotFound;

                if (takenIdentityLinks.Contains((idSource.Id, src.ExternalIdentity)))
                    return AuthErrorCatalog.ImportUserIdentitySourceLinkConflict;
            }
        }

        return null;
    }
}
