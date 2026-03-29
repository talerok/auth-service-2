using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Roles.Commands.ImportRoles;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Commands.ImportRoles;

internal sealed class ImportRolesCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<ImportRolesCommand, ImportRolesResult>
{
    public async Task<ImportRolesResult> Handle(ImportRolesCommand command, CancellationToken cancellationToken)
    {
        var permissionsByCode = await LoadAndValidatePermissions(command, cancellationToken);

        var roleNames = command.Items.Select(x => x.Name).ToList();
        var existingRoles = await dbContext.Roles
            .IgnoreQueryFilters()
            .Include(r => r.RolePermissions)
            .Where(r => roleNames.Contains(r.Name))
            .ToDictionaryAsync(r => r.Name, cancellationToken);

        var (created, updated, skipped, processed) = ApplyChanges(command, existingRoles, permissionsByCode);

        auditContext.Details = new Dictionary<string, object?>
        {
            ["count"] = command.Items.Count,
            ["created"] = created,
            ["updated"] = updated
        };

        foreach (var name in processed)
        {
            var roleId = existingRoles.TryGetValue(name, out var r) ? r.Id : dbContext.Roles.Local.First(x => x.Name == name).Id;
            await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Role, EntityId = roleId, Operation = IndexOperation.Index }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ImportRolesResult(created, updated, skipped);
    }

    private async Task<Dictionary<string, Permission>> LoadAndValidatePermissions(
        ImportRolesCommand command, CancellationToken cancellationToken)
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

        return permissionsByCode;
    }

    private (int Created, int Updated, int Skipped, List<string> Processed) ApplyChanges(
        ImportRolesCommand command, Dictionary<string, Role> existingRoles, Dictionary<string, Permission> permissionsByCode)
    {
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var processed = new List<string>();

        foreach (var item in command.Items)
        {
            if (existingRoles.TryGetValue(item.Name, out var role))
            {
                if (!command.Edit) { skipped++; continue; }
                UpdateRole(role, item, permissionsByCode);
                updated++;
            }
            else
            {
                if (!command.Add) { skipped++; continue; }
                CreateRole(item, permissionsByCode);
                created++;
            }

            processed.Add(item.Name);
        }

        return (created, updated, skipped, processed);
    }

    private void UpdateRole(Role role, ImportRoleItem item, Dictionary<string, Permission> permissionsByCode)
    {
        role.Code = item.Code;
        role.Description = item.Description;
        role.Restore();

        dbContext.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var code in item.Permissions)
            dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionsByCode[code].Id });
    }

    private void CreateRole(ImportRoleItem item, Dictionary<string, Permission> permissionsByCode)
    {
        var role = new Role
        {
            Name = item.Name,
            Code = item.Code,
            Description = item.Description
        };
        dbContext.Roles.Add(role);
        foreach (var code in item.Permissions)
            dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionsByCode[code].Id });
    }
}
