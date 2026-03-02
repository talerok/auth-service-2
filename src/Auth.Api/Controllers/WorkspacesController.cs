using Auth.Application;
using Auth.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public sealed class WorkspacesController(IWorkspaceService workspaceService, ISearchService searchService) : ControllerBase
{
    [HttpGet]
    [HasPermissionIn("system", "system.workspaces.view")]
    public Task<IReadOnlyCollection<WorkspaceDto>> GetAll(CancellationToken cancellationToken) =>
        workspaceService.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system.workspaces.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await workspaceService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system.workspaces.create")]
    public Task<WorkspaceDto> Create([FromBody] CreateWorkspaceRequest request, CancellationToken cancellationToken) =>
        workspaceService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system.workspaces.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var updated = await workspaceService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("system", "system.workspaces.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var updated = await workspaceService.PatchAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system.workspaces.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await workspaceService.SoftDeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("search")]
    [HasPermissionIn("system", "system.workspaces.view")]
    public Task<SearchResponse<WorkspaceDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        searchService.SearchWorkspacesAsync(request, cancellationToken);
}
