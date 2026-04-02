using Auth.Application;
using Auth.Application.Sessions;
using Auth.Application.Sessions.Commands.RevokeSession;
using Auth.Application.Sessions.Commands.RevokeUserSessions;
using Auth.Application.Sessions.Queries.GetUserSessions;
using Auth.Application.Sessions.Queries.SearchSessions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("")]
public sealed class SessionsController(ISender sender) : ControllerBase
{
    [HttpPost("api/sessions/search")]
    [HasSystemPermission("system.sessions.view")]
    public Task<SearchResponse<UserSessionSearchDto>> Search([FromBody] SearchRequest request, CancellationToken ct) =>
        sender.Send(new SearchSessionsQuery(request), ct);

    [HttpGet("api/users/{userId:guid}/sessions")]
    [HasSystemPermission("system.sessions.view")]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<UserSessionResponse>>> GetUserSessions(
        Guid userId, CancellationToken ct) =>
        Ok(await sender.Send(new GetUserSessionsQuery(userId), ct));

    [HttpDelete("api/users/{userId:guid}/sessions/{id:guid}")]
    [HasSystemPermission("system.sessions.revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeSession(
        Guid userId, Guid id, CancellationToken ct)
    {
        await sender.Send(new RevokeSessionCommand(id, "admin"), ct);
        return NoContent();
    }

    [HttpDelete("api/users/{userId:guid}/sessions")]
    [HasSystemPermission("system.sessions.revoke-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeAllUserSessions(
        Guid userId, CancellationToken ct)
    {
        await sender.Send(new RevokeUserSessionsCommand(userId, "admin"), ct);
        return NoContent();
    }
}
