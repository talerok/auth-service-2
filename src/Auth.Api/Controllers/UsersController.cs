using Auth.Application;
using Auth.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IUserService userService, ISearchService searchService) : ControllerBase
{
    [HttpGet]
    [HasPermissionIn("default", "users.view")]
    public Task<IReadOnlyCollection<UserDto>> GetAll(CancellationToken cancellationToken) =>
        userService.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("default", "users.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await userService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("default", "users.create")]
    public Task<UserDto> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken) =>
        userService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("default", "users.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var updated = await userService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("default", "users.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchUserRequest request, CancellationToken cancellationToken)
    {
        var updated = await userService.PatchAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("default", "users.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await userService.SoftDeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/workspaces")]
    [HasPermissionIn("default", "users.view")]
    public async Task<IActionResult> GetWorkspaces(Guid id, CancellationToken cancellationToken)
    {
        var workspaces = await userService.GetWorkspacesAsync(id, cancellationToken);
        return workspaces is null ? NotFound() : Ok(workspaces);
    }

    [HttpPut("{id:guid}/workspaces")]
    [HasPermissionIn("default", "users.update")]
    public async Task<IActionResult> SetWorkspaces(Guid id, [FromBody] SetUserWorkspacesRequest request, CancellationToken cancellationToken)
    {
        await userService.SetWorkspacesAsync(id, request.Workspaces, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    [HasPermissionIn("default", "users.reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await userService.ResetPasswordAsync(id, request.Password, cancellationToken);
        return result ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/identity-sources")]
    [HasPermissionIn("default", "users.view")]
    public async Task<IActionResult> GetIdentitySourceLinks(Guid id, CancellationToken cancellationToken)
    {
        var links = await userService.GetIdentitySourceLinksAsync(id, cancellationToken);
        return links is null ? NotFound() : Ok(links);
    }

    [HttpPost("search")]
    [HasPermissionIn("default", "users.view")]
    public Task<SearchResponse<UserDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        searchService.SearchUsersAsync(request, cancellationToken);
}
