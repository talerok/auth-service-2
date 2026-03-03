using Auth.Application;
using Auth.Application.ApiClients.Commands.UpdateApiClient;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.ApiClients.Commands.UpdateApiClient;

internal sealed class UpdateApiClientCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictApplicationManager appManager) : IRequestHandler<UpdateApiClientCommand, ApiClientDto?>
{
    public async Task<ApiClientDto?> Handle(UpdateApiClientCommand command, CancellationToken cancellationToken)
    {
        var apiClient = await dbContext.ApiClients.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (apiClient is null)
            return null;

        apiClient.Name = command.Name;
        apiClient.Description = command.Description;
        apiClient.IsActive = command.IsActive;
        apiClient.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var oidcApp = await appManager.FindByClientIdAsync(apiClient.ClientId, cancellationToken);
        if (oidcApp is not null)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);
            descriptor.DisplayName = command.Name;
            await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
        }

        var dto = new ApiClientDto(apiClient.Id, apiClient.Name, apiClient.Description, apiClient.ClientId, apiClient.IsActive);
        await searchIndexService.IndexApiClientAsync(dto, cancellationToken);
        return dto;
    }
}
