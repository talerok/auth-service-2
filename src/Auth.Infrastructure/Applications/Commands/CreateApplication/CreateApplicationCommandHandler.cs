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
    ICorsOriginService corsOriginService,
    IOpenIddictApplicationManager appManager) : IRequestHandler<CreateApplicationCommand, CreateApplicationResponse>
{
    private static readonly List<string> DefaultScopes = ["email", "profile"];
    private static readonly List<string> DefaultGrantTypes = ["authorization_code", "refresh_token"];

    public async Task<CreateApplicationResponse> Handle(CreateApplicationCommand command, CancellationToken cancellationToken)
    {
        var clientId = $"ac-{Guid.NewGuid():N}";
        var isConfidential = command.IsConfidential;
        var clientSecret = isConfidential ? GenerateSecret() : null;
        var scopes = command.Scopes is { Count: > 0 } ? command.Scopes : DefaultScopes;
        var grantTypes = command.GrantTypes is { Count: > 0 } ? command.GrantTypes : DefaultGrantTypes;

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
            PostLogoutRedirectUris = command.PostLogoutRedirectUris ?? [],
            AllowedOrigins = command.AllowedOrigins ?? [],
            Scopes = scopes,
            GrantTypes = grantTypes,
            AccessTokenLifetimeMinutes = command.AccessTokenLifetimeMinutes,
            RefreshTokenLifetimeMinutes = command.RefreshTokenLifetimeMinutes
        };

        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync(cancellationToken);
        corsOriginService.InvalidateCache();

        var descriptor = BuildDescriptor(command, clientId, clientSecret, scopes, grantTypes);
        await appManager.CreateAsync(descriptor, cancellationToken);

        var dto = MapToDto(application);
        await searchIndexService.IndexApplicationAsync(dto, cancellationToken);
        return new CreateApplicationResponse(dto, clientSecret);
    }

    private static OpenIddictApplicationDescriptor BuildDescriptor(
        CreateApplicationCommand command, string clientId, string? clientSecret,
        List<string> scopes, List<string> grantTypes)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = command.Name,
            ClientType = command.IsConfidential ? ClientTypes.Confidential : ClientTypes.Public
        };

        GrantTypeMapper.ApplyGrantTypes(descriptor, grantTypes);

        foreach (var scope in scopes)
            descriptor.Permissions.Add(OidcPermissions.Prefixes.Scope + scope);

        foreach (var uri in command.RedirectUris ?? [])
            descriptor.RedirectUris.Add(new Uri(uri));

        foreach (var uri in command.PostLogoutRedirectUris ?? [])
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri));

        GrantTypeMapper.ApplyTokenLifetimes(descriptor,
            command.AccessTokenLifetimeMinutes, command.RefreshTokenLifetimeMinutes);

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
            c.RedirectUris, c.PostLogoutRedirectUris, c.AllowedOrigins, c.Scopes,
            c.GrantTypes, c.AccessTokenLifetimeMinutes, c.RefreshTokenLifetimeMinutes);

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
