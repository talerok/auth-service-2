using System.Security.Cryptography;
using Auth.Application;
using Auth.Application.ServiceAccounts.Commands.RegenerateServiceAccountSecret;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.ServiceAccounts.Commands.RegenerateServiceAccountSecret;

internal sealed class RegenerateServiceAccountSecretCommandHandler(
    AuthDbContext dbContext,
    IOpenIddictApplicationManager appManager,
    #pragma warning disable CS9113
    IAuditContext auditContext
    #pragma warning restore CS9113
    ) : IRequestHandler<RegenerateServiceAccountSecretCommand, RegenerateServiceAccountSecretResponse?>
{
    public async Task<RegenerateServiceAccountSecretResponse?> Handle(RegenerateServiceAccountSecretCommand command, CancellationToken cancellationToken)
    {
        var serviceAccount = await dbContext.ServiceAccounts.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (serviceAccount is null)
            return null;

        var newSecret = GenerateSecret();

        var oidcApp = await appManager.FindByClientIdAsync(serviceAccount.ClientId, cancellationToken);
        if (oidcApp is not null)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);
            descriptor.ClientSecret = newSecret;
            await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
        }

        return new RegenerateServiceAccountSecretResponse(newSecret);
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
