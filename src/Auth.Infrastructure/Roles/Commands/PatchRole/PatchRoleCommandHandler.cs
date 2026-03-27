using Auth.Application;
using Auth.Application.Roles.Commands.PatchRole;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Commands.PatchRole;

internal sealed class PatchRoleCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IAuditContext auditContext) : IRequestHandler<PatchRoleCommand, RoleDto?>
{
    public async Task<RoleDto?> Handle(PatchRoleCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (command.Name.HasValue)
            entity.Name = command.Name.Value!;

        if (command.Code.HasValue)
            entity.Code = command.Code.Value!;

        if (command.Description.HasValue)
            entity.Description = command.Description.Value!;

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(entity));
        if (changes.Count > 0)
            auditContext.Details = changes;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new RoleDto(entity.Id, entity.Name, entity.Code, entity.Description);
        await searchIndexService.IndexRoleAsync(dto, cancellationToken);
        return dto;
    }
}
