using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Permissions.Commands.ImportPermissions;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Commands.ImportPermissions;

internal sealed class ImportPermissionsCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<ImportPermissionsCommand, ImportPermissionsResult>
{
    public async Task<ImportPermissionsResult> Handle(ImportPermissionsCommand command, CancellationToken cancellationToken)
    {
        var domains = command.Items.Select(x => x.Domain).Distinct().ToList();
        var existing = await dbContext.Permissions
            .IgnoreQueryFilters()
            .Where(x => domains.Contains(x.Domain))
            .ToListAsync(cancellationToken);

        existing = existing
            .Where(x => command.Items.Any(k => k.Domain == x.Domain && k.Bit == x.Bit))
            .ToList();

        var existingLookup = existing.ToDictionary(x => (x.Domain, x.Bit));

        var systemConflicts = command.Items
            .Where(item => existingLookup.TryGetValue((item.Domain, item.Bit), out var e) && e.IsSystem)
            .ToList();
        if (systemConflicts.Count > 0)
            throw new AuthException(AuthErrorCatalog.SystemPermissionImportForbidden);

        var (created, updated, skipped, processed) = ApplyChanges(command, existingLookup);

        auditContext.Details = new Dictionary<string, object?>
        {
            ["count"] = command.Items.Count,
            ["created"] = created,
            ["updated"] = updated
        };

        foreach (var key in processed)
        {
            var entityId = existingLookup.TryGetValue(key, out var ex) ? ex.Id : dbContext.Permissions.Local.First(x => x.Domain == key.Domain && x.Bit == key.Bit).Id;
            await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Permission, EntityId = entityId, Operation = IndexOperation.Index }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ImportPermissionsResult(created, updated, skipped);
    }

    private (int Created, int Updated, int Skipped, List<(string Domain, int Bit)> Processed) ApplyChanges(
        ImportPermissionsCommand command, Dictionary<(string, int), Permission> existing)
    {
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var processed = new List<(string Domain, int Bit)>();

        foreach (var item in command.Items)
        {
            if (existing.TryGetValue((item.Domain, item.Bit), out var entity))
            {
                if (!command.Edit) { skipped++; continue; }
                UpdatePermission(entity, item);
                updated++;
            }
            else
            {
                if (!command.Add) { skipped++; continue; }
                CreatePermission(item);
                created++;
            }

            processed.Add((item.Domain, item.Bit));
        }

        return (created, updated, skipped, processed);
    }

    private static void UpdatePermission(Permission entity, ImportPermissionItem item)
    {
        entity.Code = item.Code;
        entity.Description = item.Description;
        entity.Restore();
    }

    private void CreatePermission(ImportPermissionItem item)
    {
        dbContext.Permissions.Add(new Permission
        {
            Domain = item.Domain,
            Bit = item.Bit,
            Code = item.Code,
            Description = item.Description,
            IsSystem = false
        });
    }
}
