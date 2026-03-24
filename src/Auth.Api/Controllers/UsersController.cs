using System.Text.Json;
using Auth.Application;
using Auth.Application.Users.Commands.CreateUser;
using Auth.Application.Users.Commands.PatchUser;
using Auth.Application.Users.Commands.ResetPassword;
using Auth.Application.Users.Commands.SetUserIdentitySourceLinks;
using Auth.Application.Users.Commands.SetUserWorkspaces;
using Auth.Application.Users.Commands.SoftDeleteUser;
using Auth.Application.Users.Commands.UpdateUser;
using Auth.Application.Users.Queries.GetAllUsers;
using Auth.Application.Users.Queries.GetUserById;
using Auth.Application.Users.Queries.GetUserIdentitySourceLinks;
using Auth.Application.Users.Queries.GetUserWorkspaces;
using Auth.Application.Users.Commands.ImportUsers;
using Auth.Application.Users.Queries.ExportUsers;
using Auth.Application.Users.Queries.SearchUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(ISender sender) : ControllerBase
{
    private static JsonSerializerOptions JsonOptions => JsonDefaults.IndentedCamelCase;

    [HttpGet]
    [HasPermissionIn("system", "system", "system.users.view")]
    public Task<IReadOnlyCollection<UserDto>> GetAll(CancellationToken cancellationToken) =>
        sender.Send(new GetAllUsersQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermissionIn("system", "system", "system.users.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermissionIn("system", "system", "system.users.create")]
    public Task<UserDto> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken) =>
        sender.Send(new CreateUserCommand(
            request.Username, request.FullName, request.Email, request.Password,
            request.Phone, request.IsActive, request.IsInternalAuthEnabled, request.MustChangePassword,
            request.TwoFactorEnabled, request.TwoFactorChannel), cancellationToken);

    [HttpPut("{id:guid}")]
    [HasPermissionIn("system", "system", "system.users.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new UpdateUserCommand(
            id, request.Username, request.FullName, request.Email,
            request.Phone, request.IsActive, request.IsInternalAuthEnabled, request.TwoFactorEnabled,
            request.TwoFactorChannel), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}")]
    [HasPermissionIn("system", "system", "system.users.update")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchUserRequest request, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new PatchUserCommand(
            id, request.Username, request.FullName, request.Email,
            request.Phone, request.IsActive, request.IsInternalAuthEnabled, request.TwoFactorEnabled,
            request.TwoFactorChannel), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HasPermissionIn("system", "system", "system.users.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(new SoftDeleteUserCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/workspaces")]
    [HasPermissionIn("system", "system", "system.users.view")]
    public async Task<IActionResult> GetWorkspaces(Guid id, CancellationToken cancellationToken)
    {
        var workspaces = await sender.Send(new GetUserWorkspacesQuery(id), cancellationToken);
        return workspaces is null ? NotFound() : Ok(workspaces);
    }

    [HttpPut("{id:guid}/workspaces")]
    [HasPermissionIn("system", "system", "system.users.update")]
    public async Task<IActionResult> SetWorkspaces(Guid id, [FromBody] SetUserWorkspacesRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SetUserWorkspacesCommand(id, request.Workspaces), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    [HasPermissionIn("system", "system", "system.users.reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResetPasswordCommand(id, request.Password), cancellationToken);
        return result ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/identity-sources")]
    [HasPermissionIn("system", "system", "system.users.view")]
    public async Task<IActionResult> GetIdentitySourceLinks(Guid id, CancellationToken cancellationToken)
    {
        var links = await sender.Send(new GetUserIdentitySourceLinksQuery(id), cancellationToken);
        return links is null ? NotFound() : Ok(links);
    }

    [HttpPut("{id:guid}/identity-sources")]
    [HasPermissionIn("system", "system", "system.users.update")]
    public async Task<IActionResult> SetIdentitySourceLinks(Guid id, [FromBody] SetUserIdentitySourceLinksRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SetUserIdentitySourceLinksCommand(id, request.Links), cancellationToken);
        return NoContent();
    }

    [HttpPost("search")]
    [HasPermissionIn("system", "system", "system.users.view")]
    public Task<SearchResponse<UserDto>> Search([FromBody] SearchRequest request, CancellationToken cancellationToken) =>
        sender.Send(new SearchUsersQuery(request), cancellationToken);

    [HttpGet("export")]
    [HasPermissionIn("system", "system", "system.users.export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var items = await sender.Send(new ExportUsersQuery(), cancellationToken);
        var json = JsonSerializer.SerializeToUtf8Bytes(items, JsonOptions);
        return File(json, "application/json", "users.json");
    }

    [HttpPost("import")]
    [HasPermissionIn("system", "system", "system.users.import")]
    public async Task<ImportUsersResult> Import(IFormFile file, [FromQuery] bool add = true, [FromQuery] bool edit = true, [FromQuery] bool blockMissing = false, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream();
        var items = await JsonSerializer.DeserializeAsync<IReadOnlyCollection<ImportUserItem>>(stream,
            JsonOptions, cancellationToken)
            ?? [];
        return await sender.Send(new ImportUsersCommand(items, add, edit, blockMissing), cancellationToken);
    }
}
