using Auth.Application;
using Auth.Application.IdentitySources.Commands.CreateIdentitySource;
using Auth.Application.IdentitySources.Commands.CreateIdentitySourceLink;
using Auth.Application.IdentitySources.Commands.DeleteIdentitySource;
using Auth.Application.IdentitySources.Commands.DeleteIdentitySourceLink;
using Auth.Application.IdentitySources.Commands.UpdateIdentitySource;
using Auth.Application.IdentitySources.Queries.GetAllIdentitySources;
using Auth.Application.IdentitySources.Queries.GetIdentitySourceById;
using Auth.Application.IdentitySources.Queries.GetIdentitySourceLinks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/identity-sources")]
[Authorize]
public sealed class IdentitySourcesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermissionIn("system", "system.identity-sources.view")]
    public Task<IReadOnlyCollection<IdentitySourceDto>> GetAll(CancellationToken cancellationToken) =>
        sender.Send(new GetAllIdentitySourcesQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system.identity-sources.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new GetIdentitySourceByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system.identity-sources.create")]
    public Task<IdentitySourceDetailDto> Create([FromBody] CreateIdentitySourceRequest request, CancellationToken cancellationToken) =>
        sender.Send(new CreateIdentitySourceCommand(request.Name, request.DisplayName, request.Type, request.OidcConfig, request.LdapConfig), cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system.identity-sources.update")]
    public Task<IdentitySourceDetailDto> Update(Guid id, [FromBody] UpdateIdentitySourceRequest request, CancellationToken cancellationToken) =>
        sender.Send(new UpdateIdentitySourceCommand(id, request.DisplayName, request.IsEnabled, request.OidcConfig, request.LdapConfig), cancellationToken);

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system.identity-sources.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteIdentitySourceCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/links")]
    [HasPermissionIn("system", "system.identity-sources.view")]
    public Task<IReadOnlyCollection<IdentitySourceLinkDto>> GetLinks(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new GetIdentitySourceLinksQuery(id), cancellationToken);

    [HttpPost("{id:guid}/links")]
    [HasPermissionIn("system", "system.identity-sources.update")]
    public Task<IdentitySourceLinkDto> CreateLink(Guid id, [FromBody] CreateIdentitySourceLinkRequest request, CancellationToken cancellationToken) =>
        sender.Send(new CreateIdentitySourceLinkCommand(id, request.UserId, request.ExternalIdentity), cancellationToken);

    [HttpDelete("{id:guid}/links/{linkId:guid}")]
    [HasPermissionIn("system", "system.identity-sources.update")]
    public async Task<IActionResult> DeleteLink(Guid id, Guid linkId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteIdentitySourceLinkCommand(id, linkId), cancellationToken);
        return NoContent();
    }
}
