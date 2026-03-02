using Auth.Application;
using Auth.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public sealed class RolesController(IRoleService roleService, ISearchService searchService) : ControllerBase
{
    [HttpGet]
    [HasPermissionIn("system", "system.roles.view")]
    public Task<IReadOnlyCollection<RoleDto>> GetAll(CancellationToken cancellationToken) =>
        roleService.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system.roles.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await roleService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system.roles.create")]
    public Task<RoleDto> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken) =>
        roleService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system.roles.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var updated = await roleService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("system", "system.roles.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchRoleRequest request, CancellationToken cancellationToken)
    {
        var updated = await roleService.PatchAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system.roles.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await roleService.SoftDeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/permissions")]
    [HasPermissionIn("system", "system.roles.view")]
    public async Task<IActionResult> GetPermissions(Guid id, CancellationToken cancellationToken)
    {
        var permissions = await roleService.GetPermissionsAsync(id, cancellationToken);
        return permissions is null ? NotFound() : Ok(permissions);
    }

    [HttpPut("{id:guid}/permissions")]
    [HasPermissionIn("system", "system.roles.update")]
    public async Task<IActionResult> SetPermissions(Guid id, [FromBody] SetPermissionsRequest request, CancellationToken cancellationToken)
    {
        await roleService.SetPermissionsAsync(id, request.Permissions, cancellationToken);
        return NoContent();
    }

    [HttpPost("search")]
    [HasPermissionIn("system", "system.roles.view")]
    public Task<SearchResponse<RoleDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        searchService.SearchRolesAsync(request, cancellationToken);
}
