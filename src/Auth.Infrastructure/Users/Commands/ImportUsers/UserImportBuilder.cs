using Auth.Application;
using Auth.Domain;

namespace Auth.Infrastructure.Users.Commands.ImportUsers;

internal sealed class UserImportBuilder(AuthDbContext dbContext, IPasswordHasher passwordHasher)
{
    public User CreateUser(
        ImportUserItem item,
        string tempPassword,
        Dictionary<string, Workspace> workspacesByCode,
        Dictionary<string, Role> rolesByCode,
        Dictionary<string, IdentitySource> identitySourcesByCode)
    {
        var user = new User
        {
            Username = item.Username,
            FullName = item.FullName,
            Email = item.Email,
            Phone = item.Phone,
            PasswordHash = passwordHasher.Hash(tempPassword),
            IsActive = item.IsActive,
            IsInternalAuthEnabled = item.IsInternalAuthEnabled
        };

        user.MarkMustChangePassword();

        if (item.TwoFactorEnabled)
            user.EnableTwoFactor(item.TwoFactorChannel ?? TwoFactorChannel.Email);

        dbContext.Users.Add(user);

        if (item.Workspaces is not null)
            AddWorkspaces(user, item.Workspaces, workspacesByCode, rolesByCode);

        if (item.IdentitySources is not null)
            AddIdentitySourceLinks(user, item.IdentitySources, identitySourcesByCode);

        return user;
    }

    public void UpdateUser(
        User user,
        ImportUserItem item,
        Dictionary<string, Workspace> workspacesByCode,
        Dictionary<string, Role> rolesByCode,
        Dictionary<string, IdentitySource> identitySourcesByCode,
        List<IdentitySourceLink> existingLinks)
    {
        user.FullName = item.FullName;
        user.Email = item.Email;
        user.Phone = item.Phone;
        user.IsActive = item.IsActive;
        user.IsInternalAuthEnabled = item.IsInternalAuthEnabled;
        user.DeletedAt = null;

        if (item.MustChangePassword)
            user.MarkMustChangePassword();
        else
            user.ClearMustChangePassword();

        if (item.TwoFactorEnabled)
            user.EnableTwoFactor(item.TwoFactorChannel ?? TwoFactorChannel.Email);
        else
            user.DisableTwoFactor();

        if (item.Workspaces is not null)
            SyncWorkspaces(user, item.Workspaces, workspacesByCode, rolesByCode);

        if (item.IdentitySources is not null)
            SyncIdentitySourceLinks(user, item.IdentitySources, identitySourcesByCode, existingLinks);
    }

    private void AddWorkspaces(
        User user,
        IReadOnlyCollection<ImportUserWorkspaceItem> workspaces,
        Dictionary<string, Workspace> workspacesByCode,
        Dictionary<string, Role> rolesByCode)
    {
        foreach (var ws in workspaces)
        {
            var workspace = workspacesByCode[ws.WorkspaceCode];
            var userWorkspace = new UserWorkspace { UserId = user.Id, WorkspaceId = workspace.Id };
            dbContext.UserWorkspaces.Add(userWorkspace);

            foreach (var roleCode in ws.RoleCodes)
            {
                var role = rolesByCode[roleCode];
                dbContext.UserWorkspaceRoles.Add(new UserWorkspaceRole
                {
                    UserWorkspaceId = userWorkspace.Id,
                    RoleId = role.Id
                });
            }
        }
    }

    private void SyncWorkspaces(
        User user,
        IReadOnlyCollection<ImportUserWorkspaceItem> workspaces,
        Dictionary<string, Workspace> workspacesByCode,
        Dictionary<string, Role> rolesByCode)
    {
        var existingUws = user.UserWorkspaces.ToList();
        foreach (var uw in existingUws)
            dbContext.UserWorkspaceRoles.RemoveRange(uw.UserWorkspaceRoles);
        dbContext.UserWorkspaces.RemoveRange(existingUws);

        AddWorkspaces(user, workspaces, workspacesByCode, rolesByCode);
    }

    private void AddIdentitySourceLinks(
        User user,
        IReadOnlyCollection<ImportUserIdentitySourceItem> identitySources,
        Dictionary<string, IdentitySource> identitySourcesByCode)
    {
        foreach (var src in identitySources)
        {
            var identitySource = identitySourcesByCode[src.IdentitySourceCode];
            dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
            {
                UserId = user.Id,
                IdentitySourceId = identitySource.Id,
                ExternalIdentity = src.ExternalIdentity
            });
        }
    }

    private void SyncIdentitySourceLinks(
        User user,
        IReadOnlyCollection<ImportUserIdentitySourceItem> identitySources,
        Dictionary<string, IdentitySource> identitySourcesByCode,
        List<IdentitySourceLink> existingLinks)
    {
        var desired = identitySources.ToDictionary(
            s => identitySourcesByCode[s.IdentitySourceCode].Id,
            s => s.ExternalIdentity);

        var toRemove = existingLinks.Where(l => !desired.ContainsKey(l.IdentitySourceId)).ToList();
        dbContext.IdentitySourceLinks.RemoveRange(toRemove);

        foreach (var link in existingLinks.Where(l => desired.ContainsKey(l.IdentitySourceId)))
        {
            if (link.ExternalIdentity != desired[link.IdentitySourceId])
                link.ExternalIdentity = desired[link.IdentitySourceId];
            desired.Remove(link.IdentitySourceId);
        }

        foreach (var (sourceId, externalIdentity) in desired)
        {
            dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
            {
                UserId = user.Id,
                IdentitySourceId = sourceId,
                ExternalIdentity = externalIdentity
            });
        }
    }
}
