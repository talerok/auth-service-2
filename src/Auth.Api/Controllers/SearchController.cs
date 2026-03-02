using Auth.Application;
using Auth.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public sealed class SearchController(ISearchMaintenanceService searchMaintenanceService) : ControllerBase
{
    [HttpPost("reindex")]
    [HasPermissionIn("system", "system.search.reindex")]
    public async Task<IActionResult> Reindex(CancellationToken cancellationToken)
    {
        await searchMaintenanceService.ReindexAllAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("reindex/users")]
    [HasPermissionIn("system", "system.search.reindex")]
    public async Task<IActionResult> ReindexUsers(CancellationToken cancellationToken)
    {
        await searchMaintenanceService.ReindexUsersAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("reindex/roles")]
    [HasPermissionIn("system", "system.search.reindex")]
    public async Task<IActionResult> ReindexRoles(CancellationToken cancellationToken)
    {
        await searchMaintenanceService.ReindexRolesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("reindex/permissions")]
    [HasPermissionIn("system", "system.search.reindex")]
    public async Task<IActionResult> ReindexPermissions(CancellationToken cancellationToken)
    {
        await searchMaintenanceService.ReindexPermissionsAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("reindex/workspaces")]
    [HasPermissionIn("system", "system.search.reindex")]
    public async Task<IActionResult> ReindexWorkspaces(CancellationToken cancellationToken)
    {
        await searchMaintenanceService.ReindexWorkspacesAsync(cancellationToken);
        return NoContent();
    }
}
