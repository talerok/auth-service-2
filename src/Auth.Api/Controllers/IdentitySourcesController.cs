using Auth.Application;
using Auth.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/identity-sources")]
[Authorize]
public sealed class IdentitySourcesController(IIdentitySourceService identitySourceService) : ControllerBase
{
    [HttpGet]
    [HasPermissionIn("default", "identity-sources.view")]
    public Task<IReadOnlyCollection<IdentitySourceDto>> GetAll(CancellationToken cancellationToken) =>
        identitySourceService.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("default", "identity-sources.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await identitySourceService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("default", "identity-sources.create")]
    public Task<IdentitySourceDetailDto> Create([FromBody] CreateIdentitySourceRequest request, CancellationToken cancellationToken) =>
        identitySourceService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("default", "identity-sources.update")]
    public Task<IdentitySourceDetailDto> Update(Guid id, [FromBody] UpdateIdentitySourceRequest request, CancellationToken cancellationToken) =>
        identitySourceService.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("default", "identity-sources.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await identitySourceService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/links")]
    [HasPermissionIn("default", "identity-sources.view")]
    public Task<IReadOnlyCollection<IdentitySourceLinkDto>> GetLinks(Guid id, CancellationToken cancellationToken) =>
        identitySourceService.GetLinksAsync(id, cancellationToken);

    [HttpPost("{id:guid}/links")]
    [HasPermissionIn("default", "identity-sources.update")]
    public Task<IdentitySourceLinkDto> CreateLink(Guid id, [FromBody] CreateIdentitySourceLinkRequest request, CancellationToken cancellationToken) =>
        identitySourceService.CreateLinkAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}/links/{linkId:guid}")]
    [HasPermissionIn("default", "identity-sources.update")]
    public async Task<IActionResult> DeleteLink(Guid id, Guid linkId, CancellationToken cancellationToken)
    {
        await identitySourceService.DeleteLinkAsync(id, linkId, cancellationToken);
        return NoContent();
    }
}
