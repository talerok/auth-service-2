using Auth.Application;
using Auth.Application.Permissions.Commands.UpdatePermission;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Commands.UpdatePermission;

internal sealed class UpdatePermissionCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IAuditContext auditContext) : IRequestHandler<UpdatePermissionCommand, PermissionDto?>
{
    public async Task<PermissionDto?> Handle(UpdatePermissionCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
            return null;

        entity.GuardNotSystem();

        entity.Code = command.Code;
        entity.Description = command.Description;

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(entity));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new PermissionDto(entity.Id, entity.Domain, entity.Bit, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexPermissionAsync(dto, cancellationToken);
        return dto;
    }
}
