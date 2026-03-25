using Auth.Application;
using Auth.Application.Roles.Commands.SetRolePermissions;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Commands.SetRolePermissions;

internal sealed class SetRolePermissionsCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IAuditContext auditContext) : IRequestHandler<SetRolePermissionsCommand>
{
    public async Task Handle(SetRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        var permissionIds = command.Permissions.Select(x => x.Id).Distinct().ToArray();
        var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var current = await dbContext.RolePermissions.Where(x => x.RoleId == command.RoleId).ToListAsync(cancellationToken);
        var currentPermissionIds = current.Select(x => x.PermissionId).ToArray();
        var diff = CollectionDiff.Calculate(permissionIds, currentPermissionIds);
        var toRemove = current.Where(x => diff.ToRemove.Contains(x.PermissionId)).ToArray();

        if (toRemove.Length > 0)
        {
            dbContext.RolePermissions.RemoveRange(toRemove);
        }
        foreach (var permissionId in diff.ToAdd)
        {
            dbContext.RolePermissions.Add(new RolePermission { RoleId = command.RoleId, PermissionId = permissionId });
        }

        auditContext.Details = new Dictionary<string, object?>
        {
            ["added"] = diff.ToAdd.ToList(),
            ["removed"] = diff.ToRemove.ToList()
        };

        var permissionById = command.Permissions.ToDictionary(x => x.Id);
        var permissionsToReindex = new List<PermissionDto>();
        var existingPermissions = await dbContext.Permissions
            .Where(x => permissionIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var permission in existingPermissions)
        {
            var model = permissionById[permission.Id];
            var currentModel = new PermissionDto(permission.Id, permission.Domain, permission.Bit, permission.Code, permission.Description, permission.IsSystem);
            if (currentModel.Equals(model))
            {
                continue;
            }

            permission.Description = model.Description;
            permissionsToReindex.Add(
                new PermissionDto(permission.Id, permission.Domain, permission.Bit, permission.Code, permission.Description, permission.IsSystem));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var permission in permissionsToReindex)
        {
            await searchIndexService.IndexPermissionAsync(permission, cancellationToken);
        }
        await tx.CommitAsync(cancellationToken);
    }
}
