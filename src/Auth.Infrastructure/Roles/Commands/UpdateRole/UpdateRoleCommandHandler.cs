using Auth.Application;
using Auth.Application.Roles.Commands.UpdateRole;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Commands.UpdateRole;

internal sealed class UpdateRoleCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IAuditContext auditContext) : IRequestHandler<UpdateRoleCommand, RoleDto?>
{
    public async Task<RoleDto?> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = command.Name;
        entity.Code = command.Code;
        entity.Description = command.Description;
        var changes = AuditDiff.CaptureChanges(dbContext.Entry(entity));
        if (changes.Count > 0)
            auditContext.Details = changes;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new RoleDto(entity.Id, entity.Name, entity.Code, entity.Description);
        await searchIndexService.IndexRoleAsync(dto, cancellationToken);
        return dto;
    }
}
