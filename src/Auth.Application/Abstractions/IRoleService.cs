namespace Auth.Application;

public interface IRoleService
{
    Task<IReadOnlyCollection<RoleDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<RoleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken);
    Task<RoleDto?> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken);
    Task<RoleDto?> PatchAsync(Guid id, PatchRoleRequest request, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken);
    Task SetPermissionsAsync(Guid roleId, IReadOnlyCollection<PermissionDto> permissions, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PermissionDto>?> GetPermissionsAsync(Guid roleId, CancellationToken cancellationToken);
}
