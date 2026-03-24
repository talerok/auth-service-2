using Auth.Application;
using Auth.Application.Workspaces.Commands.UpdateWorkspace;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Commands.UpdateWorkspace;

internal sealed class UpdateWorkspaceCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IRequestHandler<UpdateWorkspaceCommand, WorkspaceDto?>
{
    public async Task<WorkspaceDto?> Handle(UpdateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.GuardNotSystem();

        entity.Name = command.Name;
        entity.Code = command.Code;
        entity.Description = command.Description;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new WorkspaceDto(entity.Id, entity.Name, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexWorkspaceAsync(dto, cancellationToken);
        return dto;
    }
}
