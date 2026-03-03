using Auth.Application;
using Auth.Application.ApiClients.Commands.PatchApiClient;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.ApiClients.Commands.PatchApiClient;

internal sealed class PatchApiClientCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictApplicationManager appManager) : IRequestHandler<PatchApiClientCommand, ApiClientDto?>
{
    public async Task<ApiClientDto?> Handle(PatchApiClientCommand command, CancellationToken cancellationToken)
    {
        var apiClient = await dbContext.ApiClients.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (apiClient is null)
            return null;

        if (command.Name is not null)
            apiClient.Name = command.Name;

        if (command.Description is not null)
            apiClient.Description = command.Description;

        if (command.IsActive.HasValue)
            apiClient.IsActive = command.IsActive.Value;

        apiClient.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (command.Name is not null)
        {
            var oidcApp = await appManager.FindByClientIdAsync(apiClient.ClientId, cancellationToken);
            if (oidcApp is not null)
            {
                var descriptor = new OpenIddictApplicationDescriptor();
                await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);
                descriptor.DisplayName = apiClient.Name;
                await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
            }
        }

        var dto = new ApiClientDto(apiClient.Id, apiClient.Name, apiClient.Description, apiClient.ClientId, apiClient.IsActive);
        await searchIndexService.IndexApiClientAsync(dto, cancellationToken);
        return dto;
    }
}
