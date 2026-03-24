using Auth.Application;
using Auth.Application.Applications.Commands.CreateApplication;
using Auth.Application.Applications.Commands.PatchApplication;
using Auth.Application.Applications.Commands.RegenerateApplicationSecret;
using Auth.Application.Applications.Commands.SoftDeleteApplication;
using Auth.Application.Applications.Commands.UpdateApplication;
using Auth.Application.Applications.Queries.GetAllApplications;
using Auth.Application.Applications.Queries.GetApplicationById;
using Auth.Application.Applications.Queries.SearchApplications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public sealed class ApplicationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermissionIn("system", "system", "system.applications.view")]
    public Task<IReadOnlyCollection<ApplicationDto>> GetAll(CancellationToken cancellationToken) =>
        sender.Send(new GetAllApplicationsQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system", "system.applications.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new GetApplicationByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system", "system.applications.create")]
    public Task<CreateApplicationResponse> Create([FromBody] CreateApplicationRequest request, CancellationToken cancellationToken) =>
        sender.Send(new CreateApplicationCommand(
            request.Name, request.Description, request.IsActive,
            request.IsConfidential, request.LogoUrl, request.HomepageUrl,
            request.RedirectUris, request.PostLogoutRedirectUris,
            request.AllowedOrigins, request.ConsentType, request.Scopes,
            request.GrantTypes, request.Audiences, request.AccessTokenLifetimeMinutes,
            request.RefreshTokenLifetimeMinutes), cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system", "system.applications.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApplicationRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new UpdateApplicationCommand(
            id, request.Name, request.Description, request.IsActive,
            request.LogoUrl, request.HomepageUrl,
            request.RedirectUris, request.PostLogoutRedirectUris,
            request.AllowedOrigins, request.ConsentType, request.Scopes,
            request.GrantTypes, request.Audiences, request.AccessTokenLifetimeMinutes,
            request.RefreshTokenLifetimeMinutes), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("system", "system", "system.applications.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchApplicationRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new PatchApplicationCommand(
            id, request.Name, request.Description, request.IsActive,
            request.LogoUrl, request.HomepageUrl,
            request.RedirectUris, request.PostLogoutRedirectUris,
            request.AllowedOrigins, request.ConsentType, request.Scopes,
            request.GrantTypes, request.Audiences, request.AccessTokenLifetimeMinutes,
            request.RefreshTokenLifetimeMinutes), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system", "system.applications.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(new SoftDeleteApplicationCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/regenerate-secret")]
    [HasPermissionIn("system", "system", "system.applications.update")]
    public async Task<IActionResult> RegenerateSecret(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegenerateApplicationSecretCommand(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("search")]
    [HasPermissionIn("system", "system", "system.applications.view")]
    public Task<SearchResponse<ApplicationDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        sender.Send(new SearchApplicationsQuery(request), cancellationToken);
}
