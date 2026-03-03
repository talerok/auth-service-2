using Auth.Application;
using Auth.Application.Workspaces.Commands.CreateWorkspace;
using Auth.Domain;
using MediatR;

namespace Auth.Infrastructure.Workspaces.Commands.CreateWorkspace;

internal sealed class CreateWorkspaceCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IRequestHandler<CreateWorkspaceCommand, WorkspaceDto>
{
    public async Task<WorkspaceDto> Handle(CreateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var entity = new Workspace { Name = command.Name, Code = command.Code, Description = command.Description, IsSystem = command.IsSystem };
        dbContext.Workspaces.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new WorkspaceDto(entity.Id, entity.Name, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexWorkspaceAsync(dto, cancellationToken);
        return dto;
    }
}
