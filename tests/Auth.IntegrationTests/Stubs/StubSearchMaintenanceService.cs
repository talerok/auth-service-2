namespace Auth.IntegrationTests.Stubs;

internal sealed class StubSearchMaintenanceService : ISearchMaintenanceService
{
    public Task EnsureIndicesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ReindexAllAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ReindexUsersAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ReindexRolesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ReindexPermissionsAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ReindexWorkspacesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ReindexApplicationsAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ReindexServiceAccountsAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
