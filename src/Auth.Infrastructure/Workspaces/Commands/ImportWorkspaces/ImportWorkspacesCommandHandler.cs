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

        var (created, updated, skipped, processed) = ApplyChanges(command, existing);

        await dbContext.SaveChangesAsync(cancellationToken);
        await IndexAsync(processed, existing, cancellationToken);

        return new ImportWorkspacesResult(created, updated, skipped);
    }

    private (int Created, int Updated, int Skipped, List<string> Processed) ApplyChanges(
        ImportWorkspacesCommand command, Dictionary<string, Workspace> existing)
    {
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var processed = new List<string>();

        foreach (var item in command.Items)
        {
            if (existing.TryGetValue(item.Code, out var workspace))
            {
                if (!command.Edit) { skipped++; continue; }
                UpdateWorkspace(workspace, item);
                updated++;
            }
            else
            {
                if (!command.Add) { skipped++; continue; }
                CreateWorkspace(item);
                created++;
            }

            processed.Add(item.Code);
        }

        return (created, updated, skipped, processed);
    }

    private static void UpdateWorkspace(Workspace workspace, ImportWorkspaceItem item)
    {
        workspace.Name = item.Name;
        workspace.Description = item.Description;
        workspace.DeletedAt = null;
        workspace.UpdatedAt = DateTime.UtcNow;
    }

    private void CreateWorkspace(ImportWorkspaceItem item)
    {
        dbContext.Workspaces.Add(new Workspace
        {
            Name = item.Name,
            Code = item.Code,
            Description = item.Description,
            IsSystem = false
        });
    }

    private async Task IndexAsync(List<string> processed, Dictionary<string, Workspace> existing, CancellationToken cancellationToken)
    {
        foreach (var code in processed)
        {
            var w = existing.TryGetValue(code, out var ex)
                ? ex
                : await dbContext.Workspaces.FirstAsync(x => x.Code == code, cancellationToken);
            await searchIndexService.IndexWorkspaceAsync(
                new WorkspaceDto(w.Id, w.Name, w.Code, w.Description, w.IsSystem), cancellationToken);
        }
    }
}
