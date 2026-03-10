using Auth.Application;
using Auth.Application.NotificationTemplates.Commands.UpdateNotificationTemplate;
using Auth.Application.NotificationTemplates.Queries.GetAllNotificationTemplates;
using Auth.Application.NotificationTemplates.Queries.GetNotificationTemplateByChannel;
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

    [HttpGet("{channel}")]
    [HasPermissionIn("system", "system", "system.notification-templates.view")]
    public async Task<IActionResult> GetByChannel(string channel, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new GetNotificationTemplateByChannelQuery(channel), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("{channel}")]
    [HasPermissionIn("system", "system", "system.notification-templates.update")]
    public async Task<IActionResult> Update(string channel, [FromBody] UpdateNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new UpdateNotificationTemplateCommand(channel, request.Subject, request.Body), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }
}
