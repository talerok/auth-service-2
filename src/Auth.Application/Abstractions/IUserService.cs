namespace Auth.Application;

public interface IUserService
{
    Task<IReadOnlyCollection<UserDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task<UserDto?> PatchAsync(Guid id, PatchUserRequest request, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken);
    Task SetWorkspacesAsync(Guid userId, IReadOnlyCollection<UserWorkspaceRolesItem> workspaces, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserWorkspaceRolesItem>?> GetWorkspacesAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserIdentitySourceLinkDto>?> GetIdentitySourceLinksAsync(Guid userId, CancellationToken cancellationToken);
}
