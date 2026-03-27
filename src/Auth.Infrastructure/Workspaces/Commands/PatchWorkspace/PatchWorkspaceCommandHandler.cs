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
        var dto = new WorkspaceDto(entity.Id, entity.Name, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexWorkspaceAsync(dto, cancellationToken);
        return dto;
    }
}
