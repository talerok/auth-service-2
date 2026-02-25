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
            .Select(x => new UserDto(x.Id, x.Username, x.Email, x.IsActive, x.MustChangePassword))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Users.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new UserDto(x.Id, x.Username, x.Email, x.IsActive, x.MustChangePassword))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = request.IsActive
        };
        if (request.MustChangePassword)
            user.MarkMustChangePassword();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new UserDto(user.Id, user.Username, user.Email, user.IsActive, user.MustChangePassword);
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
        user.Email = request.Email;
        user.IsActive = request.IsActive;
        if (request.MustChangePassword)
            user.MarkMustChangePassword();
        else
            user.ClearMustChangePassword();
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new UserDto(user.Id, user.Username, user.Email, user.IsActive, user.MustChangePassword);
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

        if (request.Email is not null)
        {
            user.Email = request.Email;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        if (request.MustChangePassword.HasValue)
        {
            if (request.MustChangePassword.Value)
                user.MarkMustChangePassword();
            else
                user.ClearMustChangePassword();
        }

        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new UserDto(user.Id, user.Username, user.Email, user.IsActive, user.MustChangePassword);
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
}
