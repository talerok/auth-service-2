using Auth.Application;
using Auth.Application.Permissions.Commands.PatchPermission;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Commands.PatchPermission;

internal sealed class PatchPermissionCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IAuditContext auditContext) : IRequestHandler<PatchPermissionCommand, PermissionDto?>
{
    public async Task<PermissionDto?> Handle(PatchPermissionCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
            return null;

        entity.GuardNotSystem();

        if (command.Code is not null)
            entity.Code = command.Code;
        if (command.Description is not null)
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
