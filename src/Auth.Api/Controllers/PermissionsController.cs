using Auth.Application;
using Auth.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public sealed class PermissionsController(IPermissionService permissionService, ISearchService searchService) : ControllerBase
{
    [HttpGet]
    [HasPermissionIn("system", "system.permissions.view")]
    public Task<IReadOnlyCollection<PermissionDto>> GetAll(CancellationToken cancellationToken) =>
        permissionService.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system.permissions.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await permissionService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system.permissions.create")]
    public Task<PermissionDto> Create([FromBody] CreatePermissionRequest request, CancellationToken cancellationToken) =>
        permissionService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system.permissions.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePermissionRequest request, CancellationToken cancellationToken)
    {
        var updated = await permissionService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("system", "system.permissions.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchPermissionRequest request, CancellationToken cancellationToken)
    {
        var updated = await permissionService.PatchAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system.permissions.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await permissionService.SoftDeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("search")]
    [HasPermissionIn("system", "system.permissions.view")]
    public Task<SearchResponse<PermissionDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        searchService.SearchPermissionsAsync(request, cancellationToken);
}
