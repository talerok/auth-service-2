using Auth.Application;
using Auth.Application.Permissions.Commands.ImportPermissions;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Commands.ImportPermissions;

internal sealed class ImportPermissionsCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IRequestHandler<ImportPermissionsCommand, ImportPermissionsResult>
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

        // Prevent importing over system permissions
        var systemConflicts = command.Items
            .Where(item => existingLookup.TryGetValue((item.Domain, item.Bit), out var e) && e.IsSystem)
            .ToList();
        if (systemConflicts.Count > 0)
            throw new AuthException(AuthErrorCatalog.SystemPermissionImportForbidden);

        var (created, updated, skipped, processed) = ApplyChanges(command, existingLookup);

        await dbContext.SaveChangesAsync(cancellationToken);
        await IndexAsync(processed, existingLookup, cancellationToken);

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
        entity.DeletedAt = null;
        entity.UpdatedAt = DateTime.UtcNow;
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

    private async Task IndexAsync(List<(string Domain, int Bit)> processed, Dictionary<(string, int), Permission> existing, CancellationToken cancellationToken)
    {
        foreach (var key in processed)
        {
            var e = existing.TryGetValue(key, out var ex)
                ? ex
                : await dbContext.Permissions.FirstAsync(x => x.Domain == key.Domain && x.Bit == key.Bit, cancellationToken);
            await searchIndexService.IndexPermissionAsync(
                new PermissionDto(e.Id, e.Domain, e.Bit, e.Code, e.Description, e.IsSystem), cancellationToken);
        }
    }
}
