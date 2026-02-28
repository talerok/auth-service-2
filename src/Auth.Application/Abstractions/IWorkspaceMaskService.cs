namespace Auth.Application;

public interface IWorkspaceMaskService
{
    Task<Dictionary<string, byte[]>> BuildWorkspaceMasksAsync(Guid userId, CancellationToken cancellationToken);
}
