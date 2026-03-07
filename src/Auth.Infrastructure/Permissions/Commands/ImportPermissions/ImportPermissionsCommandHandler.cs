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
        if (command.Items.Any(x => x.Bit < SystemPermissionCatalog.CustomBitStart))
            throw new AuthException(AuthErrorCatalog.SystemPermissionImportForbidden);

        var bits = command.Items.Select(x => x.Bit).ToList();
        var existing = await dbContext.Permissions
            .IgnoreQueryFilters()
            .Where(x => bits.Contains(x.Bit))
            .ToDictionaryAsync(x => x.Bit, cancellationToken);

        var (created, updated, skipped, processed) = ApplyChanges(command, existing);

        await dbContext.SaveChangesAsync(cancellationToken);
        await IndexAsync(processed, existing, cancellationToken);

        return new ImportPermissionsResult(created, updated, skipped);
    }

    private (int Created, int Updated, int Skipped, List<int> Processed) ApplyChanges(
        ImportPermissionsCommand command, Dictionary<int, Permission> existing)
    {
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var processed = new List<int>();

        foreach (var item in command.Items)
        {
            if (existing.TryGetValue(item.Bit, out var entity))
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

            processed.Add(item.Bit);
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
            Bit = item.Bit,
            Code = item.Code,
            Description = item.Description,
            IsSystem = false
        });
    }

    private async Task IndexAsync(List<int> processed, Dictionary<int, Permission> existing, CancellationToken cancellationToken)
    {
        foreach (var bit in processed)
        {
            var e = existing.TryGetValue(bit, out var ex)
                ? ex
                : await dbContext.Permissions.FirstAsync(x => x.Bit == bit, cancellationToken);
            await searchIndexService.IndexPermissionAsync(
                new PermissionDto(e.Id, e.Bit, e.Code, e.Description, e.IsSystem), cancellationToken);
        }
    }
}
