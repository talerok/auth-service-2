using System.Security.Cryptography;
using Auth.Application;
using Auth.Application.Applications.Commands.RegenerateApplicationSecret;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.Applications.Commands.RegenerateApplicationSecret;

internal sealed class RegenerateApplicationSecretCommandHandler(
    AuthDbContext dbContext,
    IOpenIddictApplicationManager appManager) : IRequestHandler<RegenerateApplicationSecretCommand, RegenerateApplicationSecretResponse?>
{
    public async Task<RegenerateApplicationSecretResponse?> Handle(RegenerateApplicationSecretCommand command, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (application is null)
            return null;

        var newSecret = GenerateSecret();

        var oidcApp = await appManager.FindByClientIdAsync(application.ClientId, cancellationToken);
        if (oidcApp is not null)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);
            descriptor.ClientSecret = newSecret;
            await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
        }

        return new RegenerateApplicationSecretResponse(newSecret);
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
