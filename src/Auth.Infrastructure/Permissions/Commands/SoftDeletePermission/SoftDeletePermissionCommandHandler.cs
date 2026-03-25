using Auth.Application;
using Auth.Application.Permissions.Commands.SoftDeletePermission;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Commands.SoftDeletePermission;

internal sealed class SoftDeletePermissionCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IAuditContext auditContext) : IRequestHandler<SoftDeletePermissionCommand, bool>
{
    public async Task<bool> Handle(SoftDeletePermissionCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
            return false;

        if (entity.IsSystem)
            throw new AuthException(AuthErrorCatalog.SystemPermissionDeleteForbidden);

        auditContext.Details = new Dictionary<string, object?> { ["domain"] = entity.Domain, ["code"] = entity.Code };
        entity.SoftDelete();
        await dbContext.SaveChangesAsync(cancellationToken);
        await searchIndexService.DeletePermissionAsync(command.Id, cancellationToken);
        return true;
    }
}
