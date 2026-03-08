using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Auth.Application;
using Auth.Application.Users.Commands.ImportUsers;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Commands.ImportUsers;

internal sealed class ImportUsersCommandHandler(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher,
    ISearchIndexService searchIndexService) : IRequestHandler<ImportUsersCommand, ImportUsersResult>
{
    public async Task<ImportUsersResult> Handle(ImportUsersCommand command, CancellationToken cancellationToken)
    {
        var workspacesByCode = await LoadWorkspaces(command, cancellationToken);
        var rolesByCode = await LoadRoles(command, cancellationToken);
        var identitySourcesByCode = await LoadIdentitySources(command, cancellationToken);

        var usernames = command.Items.Select(x => x.Username).ToList();
        var existingUsers = await dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserWorkspaces)
                .ThenInclude(uw => uw.UserWorkspaceRoles)
            .Where(u => usernames.Contains(u.Username))
            .ToDictionaryAsync(u => u.Username, cancellationToken);

        var existingEmails = await dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => !usernames.Contains(u.Username))
            .Select(u => u.Email)
            .ToListAsync(cancellationToken);
        var existingEmailSet = new HashSet<string>(existingEmails, StringComparer.OrdinalIgnoreCase);

        var existingUserIds = existingUsers.Values.Select(u => u.Id).ToList();
        var existingLinks = await dbContext.IdentitySourceLinks
            .Where(l => existingUserIds.Contains(l.UserId))
            .ToListAsync(cancellationToken);
        var linksByUserId = existingLinks.GroupBy(l => l.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<ImportUserResultItem>();
        var processedUserIds = new HashSet<Guid>();

        var validationContext = new ValidationContext(
            existingUsers, existingEmailSet, workspacesByCode, rolesByCode, identitySourcesByCode);

        foreach (var item in command.Items)
        {
            var error = validationContext.Validate(item);

            if (error is not null)
            {
                results.Add(new ImportUserResultItem(item.Username, null, "error", error));
                continue;
            }

            if (existingUsers.TryGetValue(item.Username, out var user))
            {
                if (!command.Edit)
                {
                    results.Add(new ImportUserResultItem(item.Username, null, "skipped", null));
                    processedUserIds.Add(user.Id);
                    continue;
                }

                UpdateUser(user, item, workspacesByCode, rolesByCode, identitySourcesByCode,
                    linksByUserId.GetValueOrDefault(user.Id) ?? []);
                processedUserIds.Add(user.Id);
                results.Add(new ImportUserResultItem(item.Username, null, "updated", null));
            }
            else
            {
                if (!command.Add)
                {
                    results.Add(new ImportUserResultItem(item.Username, null, "skipped", null));
                    continue;
                }

                var tempPassword = GenerateTemporaryPassword();
                var newUser = CreateUser(item, tempPassword, workspacesByCode, rolesByCode, identitySourcesByCode);
                processedUserIds.Add(newUser.Id);
                results.Add(new ImportUserResultItem(item.Username, tempPassword, "created", null));
            }
        }

        var blocked = 0;
        if (command.BlockMissing)
            blocked = await BlockMissingUsers(processedUserIds, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await IndexProcessedUsers(processedUserIds, existingUsers, cancellationToken);

        return new ImportUsersResult(results, blocked);
    }

    private User CreateUser(
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

    private void UpdateUser(
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
        user.UpdatedAt = DateTime.UtcNow;

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
        if (existingLinks.Count > 0)
            dbContext.IdentitySourceLinks.RemoveRange(existingLinks);

        AddIdentitySourceLinks(user, identitySources, identitySourcesByCode);
    }

    private async Task<int> BlockMissingUsers(HashSet<Guid> processedUserIds, CancellationToken cancellationToken)
    {
        var usersToBlock = await dbContext.Users
            .Where(u => u.IsActive && !processedUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        foreach (var user in usersToBlock)
        {
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
        }

        return usersToBlock.Count;
    }

    private async Task<Dictionary<string, Workspace>> LoadWorkspaces(
        ImportUsersCommand command, CancellationToken cancellationToken)
    {
        var codes = command.Items
            .Where(x => x.Workspaces is not null)
            .SelectMany(x => x.Workspaces!)
            .Select(x => x.WorkspaceCode)
            .Distinct()
            .ToList();

        if (codes.Count == 0)
            return new Dictionary<string, Workspace>();

        return await dbContext.Workspaces
            .Where(w => codes.Contains(w.Code))
            .ToDictionaryAsync(w => w.Code, cancellationToken);
    }

    private async Task<Dictionary<string, Role>> LoadRoles(
        ImportUsersCommand command, CancellationToken cancellationToken)
    {
        var codes = command.Items
            .Where(x => x.Workspaces is not null)
            .SelectMany(x => x.Workspaces!)
            .SelectMany(x => x.RoleCodes)
            .Distinct()
            .ToList();

        if (codes.Count == 0)
            return new Dictionary<string, Role>();

        return await dbContext.Roles
            .Where(r => codes.Contains(r.Code))
            .ToDictionaryAsync(r => r.Code, cancellationToken);
    }

    private async Task<Dictionary<string, IdentitySource>> LoadIdentitySources(
        ImportUsersCommand command, CancellationToken cancellationToken)
    {
        var codes = command.Items
            .Where(x => x.IdentitySources is not null)
            .SelectMany(x => x.IdentitySources!)
            .Select(x => x.IdentitySourceCode)
            .Distinct()
            .ToList();

        if (codes.Count == 0)
            return new Dictionary<string, IdentitySource>();

        return await dbContext.IdentitySources
            .Where(s => codes.Contains(s.Code))
            .ToDictionaryAsync(s => s.Code, cancellationToken);
    }

    private async Task IndexProcessedUsers(
        HashSet<Guid> processedUserIds, Dictionary<string, User> existingUsers, CancellationToken cancellationToken)
    {
        foreach (var userId in processedUserIds)
        {
            var user = existingUsers.Values.FirstOrDefault(u => u.Id == userId)
                       ?? await dbContext.Users.FirstAsync(u => u.Id == userId, cancellationToken);
            await searchIndexService.IndexUserAsync(
                new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone,
                    user.IsActive, user.IsInternalAuthEnabled, user.MustChangePassword,
                    user.TwoFactorEnabled, user.TwoFactorChannel),
                cancellationToken);
        }
    }

    internal static string GenerateTemporaryPassword()
    {
        const string chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%&*";
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return string.Create(16, bytes.ToArray(), (span, b) =>
        {
            for (var i = 0; i < span.Length; i++)
                span[i] = chars[b[i] % chars.Length];
        });
    }

    private sealed class ValidationContext(
        Dictionary<string, User> existingUsers,
        HashSet<string> existingEmailSet,
        Dictionary<string, Workspace> workspacesByCode,
        Dictionary<string, Role> rolesByCode,
        Dictionary<string, IdentitySource> identitySourcesByCode)
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
                    if (!identitySourcesByCode.ContainsKey(src.IdentitySourceCode))
                        return AuthErrorCatalog.ImportUserIdentitySourceNotFound;
                }
            }

            return null;
        }
    }
}
