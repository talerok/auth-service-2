using System.Text.Json;
using Auth.Application;
using Auth.Application.Roles.Commands.CreateRole;
using Auth.Application.Roles.Commands.ImportRoles;
using Auth.Application.Roles.Commands.PatchRole;
using Auth.Application.Roles.Commands.SetRolePermissions;
using Auth.Application.Roles.Commands.SoftDeleteRole;
using Auth.Application.Roles.Commands.UpdateRole;
using Auth.Application.Roles.Queries.ExportRoles;
using Auth.Application.Roles.Queries.GetAllRoles;
using Auth.Application.Roles.Queries.GetRoleById;
using Auth.Application.Roles.Queries.GetRolePermissions;
using Auth.Application.Roles.Queries.SearchRoles;
using Auth.Api;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public sealed class RolesController(ISender sender) : ControllerBase
{
    private static JsonSerializerOptions JsonOptions => JsonDefaults.IndentedCamelCase;

    [HttpGet]
    [HasPermissionIn("system", "system", "system.roles.view")]
    public async Task<IReadOnlyCollection<RoleDto>> GetAll(CancellationToken cancellationToken) =>
        await sender.Send(new GetAllRolesQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system", "system.roles.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new GetRoleByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system", "system.roles.create")]
    public async Task<RoleDto> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken) =>
        await sender.Send(new CreateRoleCommand(request.Name, request.Code, request.Description), cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system", "system.roles.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new UpdateRoleCommand(id, request.Name, request.Code, request.Description), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("system", "system", "system.roles.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchRoleRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new PatchRoleCommand(id, request.Name, request.Code, request.Description), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system", "system.roles.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(new SoftDeleteRoleCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/permissions")]
    [HasPermissionIn("system", "system", "system.roles.view")]
    public async Task<IActionResult> GetPermissions(Guid id, CancellationToken cancellationToken)
    {
        var permissions = await sender.Send(new GetRolePermissionsQuery(id), cancellationToken);
        return permissions is null ? NotFound() : Ok(permissions);
    }

    [HttpPut("{id:guid}/permissions")]
    [HasPermissionIn("system", "system", "system.roles.update")]
    public async Task<IActionResult> SetPermissions(Guid id, [FromBody] SetPermissionsRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SetRolePermissionsCommand(id, request.Permissions), cancellationToken);
        return NoContent();
    }

    [HttpPost("search")]
    [HasPermissionIn("system", "system", "system.roles.view")]
    public async Task<SearchResponse<RoleDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        await sender.Send(new SearchRolesQuery(request), cancellationToken);

    [HttpGet("export")]
    [HasPermissionIn("system", "system", "system.roles.export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var items = await sender.Send(new ExportRolesQuery(), cancellationToken);
        var json = JsonSerializer.SerializeToUtf8Bytes(items, JsonOptions);
        return File(json, "application/json", "roles.json");
    }

    [HttpPost("import")]
    [HasPermissionIn("system", "system", "system.roles.import")]
    public async Task<ImportRolesResult> Import(IFormFile file, [FromQuery] bool add = true, [FromQuery] bool edit = true, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream();
        var items = await JsonSerializer.DeserializeAsync<IReadOnlyCollection<ImportRoleItem>>(stream,
            JsonOptions, cancellationToken)
            ?? [];
        return await sender.Send(new ImportRolesCommand(items, add, edit), cancellationToken);
    }
}
