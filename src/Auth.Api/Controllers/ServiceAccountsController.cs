using Auth.Application;
using Auth.Application.ServiceAccounts.Commands.CreateServiceAccount;
using Auth.Application.ServiceAccounts.Commands.PatchServiceAccount;
using Auth.Application.ServiceAccounts.Commands.RegenerateServiceAccountSecret;
using Auth.Application.ServiceAccounts.Commands.SetServiceAccountWorkspaces;
using Auth.Application.ServiceAccounts.Commands.SoftDeleteServiceAccount;
using Auth.Application.ServiceAccounts.Commands.UpdateServiceAccount;
using Auth.Application.ServiceAccounts.Queries.GetAllServiceAccounts;
using Auth.Application.ServiceAccounts.Queries.GetServiceAccountById;
using Auth.Application.ServiceAccounts.Queries.GetServiceAccountWorkspaces;
using Auth.Application.ServiceAccounts.Queries.SearchServiceAccounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/service-accounts")]
[Authorize]
public sealed class ServiceAccountsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermissionIn("system", "system", "system.service-accounts.view")]
    public Task<IReadOnlyCollection<ServiceAccountDto>> GetAll(CancellationToken cancellationToken) =>
        sender.Send(new GetAllServiceAccountsQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system", "system.service-accounts.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new GetServiceAccountByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system", "system.service-accounts.create")]
    public Task<CreateServiceAccountResponse> Create([FromBody] CreateServiceAccountRequest request, CancellationToken cancellationToken) =>
        sender.Send(new CreateServiceAccountCommand(
            request.Name, request.Description, request.IsActive), cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system", "system.service-accounts.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceAccountRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new UpdateServiceAccountCommand(
            id, request.Name, request.Description, request.IsActive), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("system", "system", "system.service-accounts.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchServiceAccountRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new PatchServiceAccountCommand(
            id, request.Name, request.Description, request.IsActive), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system", "system.service-accounts.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(new SoftDeleteServiceAccountCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/workspaces")]
    [HasPermissionIn("system", "system", "system.service-accounts.view")]
    public async Task<IActionResult> GetWorkspaces(Guid id, CancellationToken cancellationToken)
    {
        var workspaces = await sender.Send(new GetServiceAccountWorkspacesQuery(id), cancellationToken);
        return workspaces is null ? NotFound() : Ok(workspaces);
    }

    [HttpPut("{id:guid}/workspaces")]
    [HasPermissionIn("system", "system", "system.service-accounts.update")]
    public async Task<IActionResult> SetWorkspaces(Guid id, [FromBody] SetServiceAccountWorkspacesRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SetServiceAccountWorkspacesCommand(id, request.Workspaces), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/regenerate-secret")]
    [HasPermissionIn("system", "system", "system.service-accounts.update")]
    public async Task<IActionResult> RegenerateSecret(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegenerateServiceAccountSecretCommand(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("search")]
    [HasPermissionIn("system", "system", "system.service-accounts.view")]
    public Task<SearchResponse<ServiceAccountDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        sender.Send(new SearchServiceAccountsQuery(request), cancellationToken);
}
