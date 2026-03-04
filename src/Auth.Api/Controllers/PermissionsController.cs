using Auth.Application;
using Auth.Application.Permissions.Commands.CreatePermission;
using Auth.Application.Permissions.Commands.ImportPermissions;
using Auth.Application.Permissions.Commands.PatchPermission;
using Auth.Application.Permissions.Commands.SoftDeletePermission;
using Auth.Application.Permissions.Commands.UpdatePermission;
using Auth.Application.Permissions.Queries.ExportPermissions;
using Auth.Application.Permissions.Queries.GetAllPermissions;
using Auth.Application.Permissions.Queries.GetPermissionById;
using Auth.Application.Permissions.Queries.SearchPermissions;
using System.Text.Json;
using Auth.Api;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public sealed class PermissionsController(ISender sender) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [HttpGet]
    [HasPermissionIn("system", "system.permissions.view")]
    public async Task<IReadOnlyCollection<PermissionDto>> GetAll(CancellationToken cancellationToken) =>
        await sender.Send(new GetAllPermissionsQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system.permissions.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new GetPermissionByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system.permissions.create")]
    public async Task<PermissionDto> Create([FromBody] CreatePermissionRequest request, CancellationToken cancellationToken) =>
        await sender.Send(new CreatePermissionCommand(request.Code, request.Description), cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system.permissions.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePermissionRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new UpdatePermissionCommand(id, request.Code, request.Description), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("system", "system.permissions.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchPermissionRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new PatchPermissionCommand(id, request.Code, request.Description), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system.permissions.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(new SoftDeletePermissionCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("search")]
    [HasPermissionIn("system", "system.permissions.view")]
    public async Task<SearchResponse<PermissionDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        await sender.Send(new SearchPermissionsQuery(request), cancellationToken);

    [HttpGet("export")]
    [HasPermissionIn("system", "system.permissions.export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var items = await sender.Send(new ExportPermissionsQuery(), cancellationToken);
        var json = JsonSerializer.SerializeToUtf8Bytes(items, JsonOptions);
        return File(json, "application/json", "permissions.json");
    }

    [HttpPost("import")]
    [HasPermissionIn("system", "system.permissions.import")]
    public async Task<ImportPermissionsResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var items = await JsonSerializer.DeserializeAsync<IReadOnlyCollection<ImportPermissionItem>>(stream,
            JsonOptions, cancellationToken)
            ?? [];
        return await sender.Send(new ImportPermissionsCommand(items), cancellationToken);
    }
}
