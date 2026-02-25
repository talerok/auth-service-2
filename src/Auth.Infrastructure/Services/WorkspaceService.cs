using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure;

public sealed class WorkspaceService(AuthDbContext dbContext, ISearchIndexService searchIndexService) : IWorkspaceService
{
    public async Task<IReadOnlyCollection<WorkspaceDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Workspaces.AsNoTracking()
            .Select(x => new WorkspaceDto(x.Id, x.Name, x.Code, x.Description, x.IsSystem))
            .ToListAsync(cancellationToken);

    public async Task<WorkspaceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Workspaces.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new WorkspaceDto(x.Id, x.Name, x.Code, x.Description, x.IsSystem))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<WorkspaceDto> CreateAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var entity = new Workspace { Name = request.Name, Code = request.Code, Description = request.Description, IsSystem = request.IsSystem };
        dbContext.Workspaces.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new WorkspaceDto(entity.Id, entity.Name, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexWorkspaceAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<WorkspaceDto?> UpdateAsync(Guid id, UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = request.Name;
        entity.Code = request.Code;
        entity.Description = request.Description;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new WorkspaceDto(entity.Id, entity.Name, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexWorkspaceAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<WorkspaceDto?> PatchAsync(Guid id, PatchWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (request.Name is not null)
        {
            entity.Name = request.Name;
        }

        if (request.Code is not null)
        {
            entity.Code = request.Code;
        }

        if (request.Description is not null)
        {
            entity.Description = request.Description;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new WorkspaceDto(entity.Id, entity.Name, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexWorkspaceAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }
        if (entity.IsSystem)
        {
            throw new AuthException(AuthErrorCatalog.SystemWorkspaceDeleteForbidden);
        }

        entity.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await searchIndexService.DeleteWorkspaceAsync(id, cancellationToken);
        return true;
    }
}
