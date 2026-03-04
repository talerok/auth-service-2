using Auth.Application;
using Auth.Application.Workspaces.Commands.ImportWorkspaces;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Commands.ImportWorkspaces;

internal sealed class ImportWorkspacesCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IRequestHandler<ImportWorkspacesCommand, ImportWorkspacesResult>
{
    public async Task<ImportWorkspacesResult> Handle(ImportWorkspacesCommand command, CancellationToken cancellationToken)
    {
        var codes = command.Items.Select(x => x.Code).ToList();
        var existing = await dbContext.Workspaces
            .IgnoreQueryFilters()
            .Where(w => codes.Contains(w.Code))
            .ToDictionaryAsync(w => w.Code, cancellationToken);

        var systemCodes = existing.Values.Where(w => w.IsSystem).Select(w => w.Code).ToList();
        if (systemCodes.Count > 0)
            throw new AuthException(AuthErrorCatalog.SystemWorkspaceImportForbidden);

        var created = 0;
        var updated = 0;

        foreach (var item in command.Items)
        {
            if (existing.TryGetValue(item.Code, out var workspace))
            {
                workspace.Name = item.Name;
                workspace.Description = item.Description;
                workspace.DeletedAt = null;
                workspace.UpdatedAt = DateTime.UtcNow;
                updated++;
            }
            else
            {
                workspace = new Workspace
                {
                    Name = item.Name,
                    Code = item.Code,
                    Description = item.Description,
                    IsSystem = false
                };
                dbContext.Workspaces.Add(workspace);
                created++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var item in command.Items)
        {
            var w = existing.TryGetValue(item.Code, out var ex) ? ex : await dbContext.Workspaces.FirstAsync(x => x.Code == item.Code, cancellationToken);
            await searchIndexService.IndexWorkspaceAsync(new WorkspaceDto(w.Id, w.Name, w.Code, w.Description, w.IsSystem), cancellationToken);
        }

        return new ImportWorkspacesResult(created, updated);
    }
}
