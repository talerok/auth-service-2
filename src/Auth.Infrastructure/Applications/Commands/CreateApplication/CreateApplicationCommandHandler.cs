using System.Security.Cryptography;
using Auth.Application;
using Auth.Application.Applications.Commands.CreateApplication;
using MediatR;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OidcPermissions = OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace Auth.Infrastructure.Applications.Commands.CreateApplication;

internal sealed class CreateApplicationCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictApplicationManager appManager) : IRequestHandler<CreateApplicationCommand, CreateApplicationResponse>
{
    public async Task<CreateApplicationResponse> Handle(CreateApplicationCommand command, CancellationToken cancellationToken)
    {
        var clientId = $"ac-{Guid.NewGuid():N}";
        var isConfidential = command.IsConfidential;
        var clientSecret = isConfidential ? GenerateSecret() : null;

        var application = new Domain.Application
        {
            Name = command.Name,
            Description = command.Description,
            ClientId = clientId,
            IsActive = command.IsActive,
            IsConfidential = isConfidential,
            LogoUrl = command.LogoUrl,
            HomepageUrl = command.HomepageUrl,
            RedirectUris = command.RedirectUris ?? [],
            PostLogoutRedirectUris = command.PostLogoutRedirectUris ?? []
        };

        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync(cancellationToken);

        var descriptor = BuildDescriptor(command, clientId, clientSecret);
        await appManager.CreateAsync(descriptor, cancellationToken);

        var dto = MapToDto(application);
        await searchIndexService.IndexApplicationAsync(dto, cancellationToken);
        return new CreateApplicationResponse(dto, clientSecret);
    }

    private static OpenIddictApplicationDescriptor BuildDescriptor(
        CreateApplicationCommand command, string clientId, string? clientSecret)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = command.Name,
            ClientType = command.IsConfidential ? ClientTypes.Confidential : ClientTypes.Public
        };

        descriptor.Permissions.UnionWith(new[]
        {
            OidcPermissions.Endpoints.Authorization,
            OidcPermissions.Endpoints.Token,
            OidcPermissions.Endpoints.EndSession,
            OidcPermissions.Endpoints.Revocation,
            OidcPermissions.GrantTypes.AuthorizationCode,
            OidcPermissions.GrantTypes.RefreshToken,
            OidcPermissions.ResponseTypes.Code,
            OidcPermissions.Scopes.Email,
            OidcPermissions.Scopes.Profile,
            OidcPermissions.Prefixes.Scope + "ws"
        });

        foreach (var uri in command.RedirectUris ?? [])
            descriptor.RedirectUris.Add(new Uri(uri));

        foreach (var uri in command.PostLogoutRedirectUris ?? [])
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri));

        descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);

        descriptor.ConsentType = command.ConsentType switch
        {
            "implicit" => ConsentTypes.Implicit,
            _ => ConsentTypes.Explicit
        };

        return descriptor;
    }

    private static ApplicationDto MapToDto(Domain.Application c) =>
        new(c.Id, c.Name, c.Description, c.ClientId, c.IsActive,
            c.IsConfidential, c.LogoUrl, c.HomepageUrl,
            c.RedirectUris, c.PostLogoutRedirectUris);

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
