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

        if (command.Name is not null)
        {
            entity.Name = command.Name;
        }

        if (command.Code is not null)
        {
            entity.Code = command.Code;
        }

        if (command.Description is not null)
        {
            entity.Description = command.Description;
        }

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(entity));
        if (changes.Count > 0)
            auditContext.Details = changes;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new RoleDto(entity.Id, entity.Name, entity.Code, entity.Description);
        await searchIndexService.IndexRoleAsync(dto, cancellationToken);
        return dto;
    }
}
