using Auth.Application;
using Auth.Application.Roles.Commands.ImportRoles;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Commands.ImportRoles;

internal sealed class ImportRolesCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IRequestHandler<ImportRolesCommand, ImportRolesResult>
{
    public async Task<ImportRolesResult> Handle(ImportRolesCommand command, CancellationToken cancellationToken)
    {
        var allPermissionCodes = command.Items
            .SelectMany(x => x.Permissions)
            .Distinct()
            .ToList();

        var permissionsByCode = await dbContext.Permissions
            .Where(p => allPermissionCodes.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code, cancellationToken);

        var missingCodes = allPermissionCodes.Except(permissionsByCode.Keys).ToList();
        if (missingCodes.Count > 0)
            throw new AuthException(AuthErrorCatalog.PermissionCodeNotFound);

        var roleNames = command.Items.Select(x => x.Name).ToList();
        var existingRoles = await dbContext.Roles
            .IgnoreQueryFilters()
            .Include(r => r.RolePermissions)
            .Where(r => roleNames.Contains(r.Name))
            .ToDictionaryAsync(r => r.Name, cancellationToken);

        var created = 0;
        var updated = 0;

        foreach (var item in command.Items)
        {
            if (existingRoles.TryGetValue(item.Name, out var role))
            {
                role.Description = item.Description;
                role.DeletedAt = null;
                role.UpdatedAt = DateTime.UtcNow;

                dbContext.RolePermissions.RemoveRange(role.RolePermissions);
                foreach (var code in item.Permissions)
                    dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionsByCode[code].Id });

                updated++;
            }
            else
            {
                role = new Role
                {
                    Name = item.Name,
                    Description = item.Description
                };
                dbContext.Roles.Add(role);
                foreach (var code in item.Permissions)
                    dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionsByCode[code].Id });

                created++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var item in command.Items)
        {
            var role = existingRoles.TryGetValue(item.Name, out var r) ? r : await dbContext.Roles.FirstAsync(x => x.Name == item.Name, cancellationToken);
            await searchIndexService.IndexRoleAsync(new RoleDto(role.Id, role.Name, role.Description), cancellationToken);
        }

        return new ImportRolesResult(created, updated);
    }
}
