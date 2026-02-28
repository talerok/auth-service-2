using Auth.Application;
using Auth.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/notification-templates")]
[Authorize]
public sealed class NotificationTemplatesController(INotificationTemplateService templateService) : ControllerBase
{
    [HttpGet]
    [HasPermissionIn("default", "notification-templates.view")]
    public Task<IReadOnlyCollection<NotificationTemplateDto>> GetAll(CancellationToken cancellationToken) =>
        templateService.GetAllAsync(cancellationToken);

    [HttpGet("{channel}")]
    [HasPermissionIn("default", "notification-templates.view")]
    public async Task<IActionResult> GetByChannel(string channel, CancellationToken cancellationToken)
    {
        var item = await templateService.GetByChannelAsync(channel, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("{channel}")]
    [HasPermissionIn("default", "notification-templates.update")]
    public async Task<IActionResult> Update(string channel, [FromBody] UpdateNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var updated = await templateService.UpdateByChannelAsync(channel, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }
}
