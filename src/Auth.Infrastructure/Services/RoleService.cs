using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure;

public sealed class RoleService(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOutboxEventWriter outboxEventWriter) : IRoleService
{
    public async Task<IReadOnlyCollection<RoleDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Roles.AsNoTracking().Select(x => new RoleDto(x.Id, x.Name, x.Description)).ToListAsync(cancellationToken);

    public async Task<RoleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Roles.AsNoTracking().Where(x => x.Id == id).Select(x => new RoleDto(x.Id, x.Name, x.Description)).FirstOrDefaultAsync(cancellationToken);

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var entity = new Role { Name = request.Name, Description = request.Description };
        dbContext.Roles.Add(entity);
        outboxEventWriter.AddRoleChanged(entity.Id, "created");
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new RoleDto(entity.Id, entity.Name, entity.Description);
        await searchIndexService.IndexRoleAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<RoleDto?> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.UpdatedAt = DateTime.UtcNow;
        outboxEventWriter.AddRoleChanged(entity.Id, "updated");
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new RoleDto(entity.Id, entity.Name, entity.Description);
        await searchIndexService.IndexRoleAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<RoleDto?> PatchAsync(Guid id, PatchRoleRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (request.Name is not null)
        {
            entity.Name = request.Name;
        }

        if (request.Description is not null)
        {
            entity.Description = request.Description;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        outboxEventWriter.AddRoleChanged(entity.Id, "updated");
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new RoleDto(entity.Id, entity.Name, entity.Description);
        await searchIndexService.IndexRoleAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.DeletedAt = DateTime.UtcNow;
        outboxEventWriter.AddRoleChanged(entity.Id, "deleted");
        await dbContext.SaveChangesAsync(cancellationToken);
        await searchIndexService.DeleteRoleAsync(id, cancellationToken);
        return true;
    }

    public async Task SetPermissionsAsync(Guid roleId, IReadOnlyCollection<PermissionDto> permissions, CancellationToken cancellationToken)
    {
        var permissionIds = permissions.Select(x => x.Id).Distinct().ToArray();
        var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var current = await dbContext.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(cancellationToken);
        var currentPermissionIds = current.Select(x => x.PermissionId).ToArray();
        var diff = CollectionDiff.Calculate(permissionIds, currentPermissionIds);
        var toRemove = current.Where(x => diff.ToRemove.Contains(x.PermissionId)).ToArray();

        if (toRemove.Length > 0)
        {
            dbContext.RolePermissions.RemoveRange(toRemove);
        }
        foreach (var permissionId in diff.ToAdd)
        {
            dbContext.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        }

        var permissionById = permissions.ToDictionary(x => x.Id);
        var permissionsToReindex = new List<PermissionDto>();
        var existingPermissions = await dbContext.Permissions
            .Where(x => permissionIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var permission in existingPermissions)
        {
            var model = permissionById[permission.Id];
            var currentModel = new PermissionDto(permission.Id, permission.Bit, permission.Code, permission.Description, permission.IsSystem);
            if (currentModel.Equals(model))
            {
                continue;
            }

            permission.Description = model.Description;
            permission.UpdatedAt = DateTime.UtcNow;
            outboxEventWriter.AddPermissionChanged(permission.Id, "updated");
            permissionsToReindex.Add(
                new PermissionDto(permission.Id, permission.Bit, permission.Code, permission.Description, permission.IsSystem));
        }

        outboxEventWriter.AddRoleChanged(roleId, "permissions.updated", permissionIds);
        await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var permission in permissionsToReindex)
        {
            await searchIndexService.IndexPermissionAsync(permission, cancellationToken);
        }
        await tx.CommitAsync(cancellationToken);
    }
}
