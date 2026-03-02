using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure;

public sealed class UserService(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher,
    ISearchIndexService searchIndexService) : IUserService
{
    public async Task<IReadOnlyCollection<UserDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users.AsNoTracking()
            .Select(x => new UserDto(x.Id, x.Username, x.FullName, x.Email, x.Phone, x.IsActive, x.MustChangePassword, x.TwoFactorEnabled, x.TwoFactorChannel))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Users.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new UserDto(x.Id, x.Username, x.FullName, x.Email, x.Phone, x.IsActive, x.MustChangePassword, x.TwoFactorEnabled, x.TwoFactorChannel))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Username = request.Username,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = request.IsActive
        };
        if (request.MustChangePassword)
            user.MarkMustChangePassword();
        if (request.TwoFactorEnabled)
            user.EnableTwoFactor(request.TwoFactorChannel ?? TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone, user.IsActive, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel);
        await searchIndexService.IndexUserAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.Username = request.Username;
        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.IsActive = request.IsActive;
        if (request.TwoFactorEnabled)
            user.EnableTwoFactor(request.TwoFactorChannel ?? TwoFactorChannel.Email);
        else
            user.DisableTwoFactor();
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone, user.IsActive, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel);
        await searchIndexService.IndexUserAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<UserDto?> PatchAsync(Guid id, PatchUserRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (request.Username is not null)
        {
            user.Username = request.Username;
        }

        if (request.FullName is not null)
        {
            user.FullName = request.FullName;
        }

        if (request.Email is not null)
        {
            user.Email = request.Email;
        }

        if (request.Phone is not null)
        {
            user.Phone = request.Phone;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        if (request.TwoFactorEnabled.HasValue)
        {
            if (request.TwoFactorEnabled.Value)
                user.EnableTwoFactor(request.TwoFactorChannel ?? TwoFactorChannel.Email);
            else
                user.DisableTwoFactor();
        }

        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone, user.IsActive, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel);
        await searchIndexService.IndexUserAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await searchIndexService.DeleteUserAsync(id, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<UserWorkspaceRolesItem>?> GetWorkspacesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
        if (!exists)
            return null;

        return await dbContext.UserWorkspaces
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new UserWorkspaceRolesItem(
                x.WorkspaceId,
                x.UserWorkspaceRoles.Select(r => r.RoleId).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task SetWorkspacesAsync(
        Guid userId,
        IReadOnlyCollection<UserWorkspaceRolesItem> workspaces,
        CancellationToken cancellationToken)
    {
        var workspacesById = workspaces
            .GroupBy(x => x.WorkSpaceId)
            .ToDictionary(
                x => x.Key,
                x => x.SelectMany(y => y.RoleIds)
                    .Distinct()
                    .ToArray());

        var workspaceIds = workspacesById.Keys.ToArray();
        var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var current = await dbContext.UserWorkspaces.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        var currentWorkspaceIds = current.Select(x => x.WorkspaceId).ToArray();
        var diff = CollectionDiff.Calculate(workspaceIds, currentWorkspaceIds);
        var toRemove = current.Where(x => diff.ToRemove.Contains(x.WorkspaceId)).ToArray();

        if (toRemove.Length > 0)
        {
            dbContext.UserWorkspaces.RemoveRange(toRemove);
        }

        var targetUserWorkspacesByWorkspaceId = current
            .Where(x => !diff.ToRemove.Contains(x.WorkspaceId))
            .ToDictionary(x => x.WorkspaceId);

        foreach (var workspaceId in diff.ToAdd)
        {
            var userWorkspace = new UserWorkspace { UserId = userId, WorkspaceId = workspaceId };
            dbContext.UserWorkspaces.Add(userWorkspace);
            targetUserWorkspacesByWorkspaceId[workspaceId] = userWorkspace;
        }

        var targetUserWorkspaceIds = targetUserWorkspacesByWorkspaceId.Values.Select(x => x.Id).ToArray();
        var currentRoles = await dbContext.UserWorkspaceRoles
            .Where(x => targetUserWorkspaceIds.Contains(x.UserWorkspaceId))
            .ToListAsync(cancellationToken);

        foreach (var workspace in workspacesById)
        {
            var userWorkspace = targetUserWorkspacesByWorkspaceId[workspace.Key];
            var requestedRoleIds = workspace.Value;
            var workspaceCurrentRoles = currentRoles.Where(x => x.UserWorkspaceId == userWorkspace.Id).ToArray();
            var currentRoleIds = workspaceCurrentRoles.Select(x => x.RoleId).ToArray();
            var roleDiff = CollectionDiff.Calculate(requestedRoleIds, currentRoleIds);
            var rolesToRemove = workspaceCurrentRoles.Where(x => roleDiff.ToRemove.Contains(x.RoleId)).ToArray();

            if (rolesToRemove.Length > 0)
            {
                dbContext.UserWorkspaceRoles.RemoveRange(rolesToRemove);
            }

            foreach (var roleId in roleDiff.ToAdd)
            {
                dbContext.UserWorkspaceRoles.Add(new UserWorkspaceRole { UserWorkspaceId = userWorkspace.Id, RoleId = roleId });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
            return false;

        user.PasswordHash = passwordHasher.Hash(newPassword);
        user.MarkMustChangePassword();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<UserIdentitySourceLinkDto>?> GetIdentitySourceLinksAsync(Guid userId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
        if (!exists)
            return null;

        return await dbContext.IdentitySourceLinks.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Join(dbContext.IdentitySources, l => l.IdentitySourceId, s => s.Id,
                (l, s) => new UserIdentitySourceLinkDto(
                    l.Id, s.Id, s.Name, s.DisplayName, s.Type, l.ExternalIdentity, l.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
