using Auth.Application;
using Auth.Application.ApiClients.Commands.CreateApiClient;
using Auth.Application.ApiClients.Commands.PatchApiClient;
using Auth.Application.ApiClients.Commands.RegenerateApiClientSecret;
using Auth.Application.ApiClients.Commands.SetApiClientWorkspaces;
using Auth.Application.ApiClients.Commands.SoftDeleteApiClient;
using Auth.Application.ApiClients.Commands.UpdateApiClient;
using Auth.Application.ApiClients.Queries.GetAllApiClients;
using Auth.Application.ApiClients.Queries.GetApiClientById;
using Auth.Application.ApiClients.Queries.GetApiClientWorkspaces;
using Auth.Application.ApiClients.Queries.SearchApiClients;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/api-clients")]
[Authorize]
public sealed class ApiClientsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermissionIn("system", "system", "system.api-clients.view")]
    public Task<IReadOnlyCollection<ApiClientDto>> GetAll(CancellationToken cancellationToken) =>
        sender.Send(new GetAllApiClientsQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system", "system.api-clients.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new GetApiClientByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system", "system.api-clients.create")]
    public Task<CreateApiClientResponse> Create([FromBody] CreateApiClientRequest request, CancellationToken cancellationToken) =>
        sender.Send(new CreateApiClientCommand(request.Name, request.Description, request.IsActive), cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system", "system.api-clients.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApiClientRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new UpdateApiClientCommand(id, request.Name, request.Description, request.IsActive), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("system", "system", "system.api-clients.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchApiClientRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new PatchApiClientCommand(id, request.Name, request.Description, request.IsActive), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system", "system.api-clients.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(new SoftDeleteApiClientCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/workspaces")]
    [HasPermissionIn("system", "system", "system.api-clients.view")]
    public async Task<IActionResult> GetWorkspaces(Guid id, CancellationToken cancellationToken)
    {
        var workspaces = await sender.Send(new GetApiClientWorkspacesQuery(id), cancellationToken);
        return workspaces is null ? NotFound() : Ok(workspaces);
    }

    [HttpPut("{id:guid}/workspaces")]
    [HasPermissionIn("system", "system", "system.api-clients.update")]
    public async Task<IActionResult> SetWorkspaces(Guid id, [FromBody] SetApiClientWorkspacesRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SetApiClientWorkspacesCommand(id, request.Workspaces), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/regenerate-secret")]
    [HasPermissionIn("system", "system", "system.api-clients.update")]
    public async Task<IActionResult> RegenerateSecret(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegenerateApiClientSecretCommand(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("search")]
    [HasPermissionIn("system", "system", "system.api-clients.view")]
    public Task<SearchResponse<ApiClientDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        sender.Send(new SearchApiClientsQuery(request), cancellationToken);
}
