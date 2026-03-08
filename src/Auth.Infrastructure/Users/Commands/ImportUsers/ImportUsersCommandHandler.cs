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

        var takenIdentityLinks = await LoadTakenIdentityLinks(command, existingUserIds, identitySourcesByCode, cancellationToken);

        var validator = new ImportUserValidator(
            existingUsers, existingEmailSet, workspacesByCode, rolesByCode, identitySourcesByCode, takenIdentityLinks);
        var builder = new UserImportBuilder(dbContext, passwordHasher);

        var results = new List<ImportUserResultItem>();
        var processedUserIds = new HashSet<Guid>();

        foreach (var item in command.Items)
        {
            var error = validator.Validate(item);

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

                builder.UpdateUser(user, item, workspacesByCode, rolesByCode, identitySourcesByCode,
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

                var tempPassword = PasswordGenerator.GenerateTemporaryPassword();
                var newUser = builder.CreateUser(item, tempPassword, workspacesByCode, rolesByCode, identitySourcesByCode);
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

    private async Task<HashSet<(Guid IdentitySourceId, string ExternalIdentity)>> LoadTakenIdentityLinks(
        ImportUsersCommand command,
        List<Guid> existingUserIds,
        Dictionary<string, IdentitySource> identitySourcesByCode,
        CancellationToken cancellationToken)
    {
        var pairs = command.Items
            .Where(x => x.IdentitySources is not null)
            .SelectMany(x => x.IdentitySources!)
            .Where(s => identitySourcesByCode.ContainsKey(s.IdentitySourceCode))
            .Select(s => (identitySourcesByCode[s.IdentitySourceCode].Id, s.ExternalIdentity))
            .ToList();

        if (pairs.Count == 0)
            return [];

        var sourceIds = pairs.Select(p => p.Id).Distinct().ToList();
        var externalIds = pairs.Select(p => p.ExternalIdentity).Distinct().ToList();

        var conflicting = await dbContext.IdentitySourceLinks
            .Where(l => !existingUserIds.Contains(l.UserId)
                        && sourceIds.Contains(l.IdentitySourceId)
                        && externalIds.Contains(l.ExternalIdentity))
            .Select(l => new { l.IdentitySourceId, l.ExternalIdentity })
            .ToListAsync(cancellationToken);

        return conflicting.Select(c => (c.IdentitySourceId, c.ExternalIdentity)).ToHashSet();
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
}
