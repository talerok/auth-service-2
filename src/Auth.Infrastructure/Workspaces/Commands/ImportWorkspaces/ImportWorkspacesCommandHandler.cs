using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Workspaces.Commands.ImportWorkspaces;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.Workspaces.Commands.ImportWorkspaces;

internal sealed class ImportWorkspacesCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IOpenIddictScopeManager scopeManager,
    IAuditContext auditContext) : IRequestHandler<ImportWorkspacesCommand, ImportWorkspacesResult>
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

        auditContext.Details = new Dictionary<string, object?>
        {
            ["count"] = command.Items.Count,
            ["created"] = created,
            ["updated"] = updated
        };

        foreach (var code in processed)
        {
            var wId = existing.TryGetValue(code, out var ex) ? ex.Id : dbContext.Workspaces.Local.First(x => x.Code == code).Id;
            await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Workspace, EntityId = wId, Operation = IndexOperation.Index }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await SyncWorkspaceScopesAsync(processed, cancellationToken);

        return new ImportWorkspacesResult(created, updated, skipped);
    }

    private async Task SyncWorkspaceScopesAsync(List<string> codes, CancellationToken cancellationToken)
    {
        foreach (var code in codes)
        {
            var scopeName = $"ws:{code}";
            if (await scopeManager.FindByNameAsync(scopeName, cancellationToken) is null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = scopeName,
                    DisplayName = $"Workspace: {code}"
                }, cancellationToken);
            }
        }
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
        workspace.Restore();
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
}
