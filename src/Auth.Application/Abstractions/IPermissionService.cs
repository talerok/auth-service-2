namespace Auth.Application;

public interface IPermissionService
{
    Task<IReadOnlyCollection<PermissionDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<PermissionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PermissionDto> CreateAsync(CreatePermissionRequest request, CancellationToken cancellationToken);
    Task<PermissionDto?> UpdateAsync(Guid id, UpdatePermissionRequest request, CancellationToken cancellationToken);
    Task<PermissionDto?> PatchAsync(Guid id, PatchPermissionRequest request, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken);
}
