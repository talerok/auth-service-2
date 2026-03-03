using System.Security.Cryptography;
using Auth.Application;
using Auth.Application.ApiClients.Commands.RegenerateApiClientSecret;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.ApiClients.Commands.RegenerateApiClientSecret;

internal sealed class RegenerateApiClientSecretCommandHandler(
    AuthDbContext dbContext,
    IOpenIddictApplicationManager appManager) : IRequestHandler<RegenerateApiClientSecretCommand, RegenerateApiClientSecretResponse?>
{
    public async Task<RegenerateApiClientSecretResponse?> Handle(RegenerateApiClientSecretCommand command, CancellationToken cancellationToken)
    {
        var apiClient = await dbContext.ApiClients.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (apiClient is null)
            return null;

        var newSecret = GenerateSecret();

        var oidcApp = await appManager.FindByClientIdAsync(apiClient.ClientId, cancellationToken);
        if (oidcApp is not null)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);
            descriptor.ClientSecret = newSecret;
            await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
        }

        return new RegenerateApiClientSecretResponse(newSecret);
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
