using Auth.Application;
using Auth.Application.ApiClients.Commands.SoftDeleteApiClient;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.ApiClients.Commands.SoftDeleteApiClient;

internal sealed class SoftDeleteApiClientCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictApplicationManager appManager) : IRequestHandler<SoftDeleteApiClientCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteApiClientCommand command, CancellationToken cancellationToken)
    {
        var apiClient = await dbContext.ApiClients.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (apiClient is null)
            return false;

        apiClient.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var oidcApp = await appManager.FindByClientIdAsync(apiClient.ClientId, cancellationToken);
        if (oidcApp is not null)
            await appManager.DeleteAsync(oidcApp, cancellationToken);

        await searchIndexService.DeleteApiClientAsync(apiClient.Id, cancellationToken);
        return true;
    }
}
