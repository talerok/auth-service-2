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

        var created = 0;
        var updated = 0;

        foreach (var item in command.Items)
        {
            if (existing.TryGetValue(item.Bit, out var entity))
            {
                entity.Code = item.Code;
                entity.Description = item.Description;
                entity.DeletedAt = null;
                entity.UpdatedAt = DateTime.UtcNow;
                updated++;
            }
            else
            {
                entity = new Permission
                {
                    Bit = item.Bit,
                    Code = item.Code,
                    Description = item.Description,
                    IsSystem = false
                };
                dbContext.Permissions.Add(entity);
                created++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var item in command.Items)
        {
            var e = existing.TryGetValue(item.Bit, out var ex) ? ex : await dbContext.Permissions.FirstAsync(x => x.Bit == item.Bit, cancellationToken);
            var dto = new PermissionDto(e.Id, e.Bit, e.Code, e.Description, e.IsSystem);
            await searchIndexService.IndexPermissionAsync(dto, cancellationToken);
        }

        return new ImportPermissionsResult(created, updated);
    }
}
