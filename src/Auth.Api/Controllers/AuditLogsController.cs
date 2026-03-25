using Auth.Application;
using Auth.Application.AuditLogs.Queries.SearchAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public sealed class AuditLogsController(ISender sender) : ControllerBase
{
    [HttpPost("search")]
    [HasPermissionIn("system", "system", "system.audit-logs.view")]
    public Task<SearchResponse<AuditLogDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        sender.Send(new SearchAuditLogsQuery(request), cancellationToken);
}
