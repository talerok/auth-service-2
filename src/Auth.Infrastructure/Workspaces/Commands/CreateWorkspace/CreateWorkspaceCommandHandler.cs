using Auth.Application;
using Auth.Application.Workspaces.Commands.CreateWorkspace;
using Auth.Domain;
using MediatR;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.Workspaces.Commands.CreateWorkspace;

internal sealed class CreateWorkspaceCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictScopeManager scopeManager) : IRequestHandler<CreateWorkspaceCommand, WorkspaceDto>
{
    public async Task<WorkspaceDto> Handle(CreateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var entity = new Workspace { Name = command.Name, Code = command.Code, Description = command.Description, IsSystem = command.IsSystem };
        dbContext.Workspaces.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = $"ws:{entity.Code}",
            DisplayName = $"Workspace: {entity.Code}"
        }, cancellationToken);

        var dto = new WorkspaceDto(entity.Id, entity.Name, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexWorkspaceAsync(dto, cancellationToken);
        return dto;
    }
}
