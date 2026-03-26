using Auth.Application;
using Auth.Application.NotificationTemplates.Commands.CreateNotificationTemplate;
using Auth.Application.NotificationTemplates.Commands.PatchNotificationTemplate;
using Auth.Application.NotificationTemplates.Commands.SoftDeleteNotificationTemplate;
using Auth.Application.NotificationTemplates.Commands.UpdateNotificationTemplate;
using Auth.Application.NotificationTemplates.Queries.GetAllNotificationTemplates;
using Auth.Application.NotificationTemplates.Queries.GetNotificationTemplateById;
using Auth.Application.NotificationTemplates.Queries.SearchNotificationTemplates;
using Auth.Api;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/notification-templates")]
[Authorize]
public sealed class NotificationTemplatesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermissionIn("system", "system", "system.notification-templates.view")]
    public async Task<IReadOnlyCollection<NotificationTemplateDto>> GetAll(CancellationToken cancellationToken) =>
        await sender.Send(new GetAllNotificationTemplatesQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system", "system.notification-templates.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new GetNotificationTemplateByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system", "system.notification-templates.create")]
    public async Task<NotificationTemplateDto> Create([FromBody] CreateNotificationTemplateRequest request, CancellationToken cancellationToken) =>
        await sender.Send(new CreateNotificationTemplateCommand(request.Type, request.Locale, request.Subject, request.Body), cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system", "system.notification-templates.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new UpdateNotificationTemplateCommand(id, request.Type, request.Locale, request.Subject, request.Body), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("system", "system", "system.notification-templates.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var patched = await sender.Send(new PatchNotificationTemplateCommand(id, request.Type, request.Locale, request.Subject, request.Body), cancellationToken);
        return patched is null ? NotFound() : Ok(patched);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system", "system.notification-templates.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(new SoftDeleteNotificationTemplateCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("search")]
    [HasPermissionIn("system", "system", "system.notification-templates.view")]
    public async Task<SearchResponse<NotificationTemplateDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        await sender.Send(new SearchNotificationTemplatesQuery(request), cancellationToken);
}
