using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure;

public sealed class PermissionService(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IPermissionService
{
    public async Task<IReadOnlyCollection<PermissionDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Permissions.AsNoTracking()
            .Select(x => new PermissionDto(x.Id, x.Bit, x.Code, x.Description, x.IsSystem))
            .ToListAsync(cancellationToken);

    public async Task<PermissionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Permissions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new PermissionDto(x.Id, x.Bit, x.Code, x.Description, x.IsSystem))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PermissionDto> CreateAsync(CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        var maxBit = await dbContext.Permissions.IgnoreQueryFilters()
            .Select(x => (int?)x.Bit)
            .MaxAsync(cancellationToken) ?? -1;

        var entity = new Permission
        {
            Bit = maxBit + 1,
            Code = request.Code,
            Description = request.Description,
            IsSystem = false
        };
        dbContext.Permissions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new PermissionDto(entity.Id, entity.Bit, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexPermissionAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<PermissionDto?> UpdateAsync(Guid id, UpdatePermissionRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Description = request.Description;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new PermissionDto(entity.Id, entity.Bit, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexPermissionAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<PermissionDto?> PatchAsync(Guid id, PatchPermissionRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (request.Description is not null)
        {
            entity.Description = request.Description;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new PermissionDto(entity.Id, entity.Bit, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexPermissionAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }
        if (entity.IsSystem)
        {
            throw new AuthException(AuthErrorCatalog.SystemPermissionDeleteForbidden);
        }

        entity.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await searchIndexService.DeletePermissionAsync(id, cancellationToken);
        return true;
    }
}
