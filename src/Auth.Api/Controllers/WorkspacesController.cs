using System.Text.Json;
using Auth.Application;
using Auth.Application.Workspaces.Commands.CreateWorkspace;
using Auth.Application.Workspaces.Commands.ImportWorkspaces;
using Auth.Application.Workspaces.Commands.PatchWorkspace;
using Auth.Application.Workspaces.Commands.SoftDeleteWorkspace;
using Auth.Application.Workspaces.Commands.UpdateWorkspace;
using Auth.Application.Workspaces.Queries.ExportWorkspaces;
using Auth.Application.Workspaces.Queries.GetAllWorkspaces;
using Auth.Application.Workspaces.Queries.GetWorkspaceById;
using Auth.Application.Workspaces.Queries.SearchWorkspaces;
using Auth.Api;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public sealed class WorkspacesController(ISender sender) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [HttpGet]
    [HasPermissionIn("system", "system.workspaces.view")]
    public async Task<IReadOnlyCollection<WorkspaceDto>> GetAll(CancellationToken cancellationToken) =>
        await sender.Send(new GetAllWorkspacesQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system.workspaces.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new GetWorkspaceByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system.workspaces.create")]
    public async Task<WorkspaceDto> Create([FromBody] CreateWorkspaceRequest request, CancellationToken cancellationToken) =>
        await sender.Send(new CreateWorkspaceCommand(request.Name, request.Code, request.Description, request.IsSystem), cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system.workspaces.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new UpdateWorkspaceCommand(id, request.Name, request.Code, request.Description), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("system", "system.workspaces.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new PatchWorkspaceCommand(id, request.Name, request.Code, request.Description), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system.workspaces.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(new SoftDeleteWorkspaceCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("search")]
    [HasPermissionIn("system", "system.workspaces.view")]
    public async Task<SearchResponse<WorkspaceDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        await sender.Send(new SearchWorkspacesQuery(request), cancellationToken);

    [HttpGet("export")]
    [HasPermissionIn("system", "system.workspaces.export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var items = await sender.Send(new ExportWorkspacesQuery(), cancellationToken);
        var json = JsonSerializer.SerializeToUtf8Bytes(items, JsonOptions);
        return File(json, "application/json", "workspaces.json");
    }

    [HttpPost("import")]
    [HasPermissionIn("system", "system.workspaces.import")]
    public async Task<ImportWorkspacesResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var items = await JsonSerializer.DeserializeAsync<IReadOnlyCollection<ImportWorkspaceItem>>(stream,
            JsonOptions, cancellationToken)
            ?? [];
        return await sender.Send(new ImportWorkspacesCommand(items), cancellationToken);
    }
}
