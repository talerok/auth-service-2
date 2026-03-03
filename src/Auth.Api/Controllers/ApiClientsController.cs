using Auth.Application;
using Auth.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/api-clients")]
[Authorize]
public sealed class ApiClientsController(IApiClientService apiClientService, ISearchService searchService) : ControllerBase
{
    [HttpGet]
    [HasPermissionIn("system", "system.api-clients.view")]
    public Task<IReadOnlyCollection<ApiClientDto>> GetAll(CancellationToken cancellationToken) =>
        apiClientService.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system.api-clients.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await apiClientService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system.api-clients.create")]
    public Task<CreateApiClientResponse> Create([FromBody] CreateApiClientRequest request, CancellationToken cancellationToken) =>
        apiClientService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system.api-clients.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApiClientRequest request, CancellationToken cancellationToken)
    {
        var updated = await apiClientService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("system", "system.api-clients.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchApiClientRequest request, CancellationToken cancellationToken)
    {
        var updated = await apiClientService.PatchAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system.api-clients.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await apiClientService.SoftDeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/workspaces")]
    [HasPermissionIn("system", "system.api-clients.view")]
    public async Task<IActionResult> GetWorkspaces(Guid id, CancellationToken cancellationToken)
    {
        var workspaces = await apiClientService.GetWorkspacesAsync(id, cancellationToken);
        return workspaces is null ? NotFound() : Ok(workspaces);
    }

    [HttpPut("{id:guid}/workspaces")]
    [HasPermissionIn("system", "system.api-clients.update")]
    public async Task<IActionResult> SetWorkspaces(Guid id, [FromBody] SetApiClientWorkspacesRequest request, CancellationToken cancellationToken)
    {
        await apiClientService.SetWorkspacesAsync(id, request.Workspaces, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/regenerate-secret")]
    [HasPermissionIn("system", "system.api-clients.update")]
    public async Task<IActionResult> RegenerateSecret(Guid id, CancellationToken cancellationToken)
    {
        var result = await apiClientService.RegenerateSecretAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("search")]
    [HasPermissionIn("system", "system.api-clients.view")]
    public Task<SearchResponse<ApiClientDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        searchService.SearchApiClientsAsync(request, cancellationToken);
}
