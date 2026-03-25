using Auth.Application;
using Auth.Application.Workspaces.Commands.PatchWorkspace;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Commands.PatchWorkspace;

internal sealed class PatchWorkspaceCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IAuditContext auditContext) : IRequestHandler<PatchWorkspaceCommand, WorkspaceDto?>
{
    public async Task<WorkspaceDto?> Handle(PatchWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.GuardNotSystem();

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
        var dto = new WorkspaceDto(entity.Id, entity.Name, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexWorkspaceAsync(dto, cancellationToken);
        return dto;
    }
}
