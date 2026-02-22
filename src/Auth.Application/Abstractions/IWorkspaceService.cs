namespace Auth.Application;

public interface IWorkspaceService
{
    Task<IReadOnlyCollection<WorkspaceDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<WorkspaceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkspaceDto> CreateAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken);
    Task<WorkspaceDto?> UpdateAsync(Guid id, UpdateWorkspaceRequest request, CancellationToken cancellationToken);
    Task<WorkspaceDto?> PatchAsync(Guid id, PatchWorkspaceRequest request, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken);
}
