using System.Security.Cryptography;
using Auth.Application;
using Auth.Application.Applications.Commands.CreateApplication;
using Auth.Application.Messaging.Commands;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OidcPermissions = OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace Auth.Infrastructure.Applications.Commands.CreateApplication;

internal sealed class CreateApplicationCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    ICorsOriginService corsOriginService,
    IOpenIddictApplicationManager appManager,
    IAuditContext auditContext) : IRequestHandler<CreateApplicationCommand, CreateApplicationResponse>
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
            Id = command.EntityId,
            Name = command.Name,
            Description = command.Description,
            ClientId = clientId,
            IsActive = command.IsActive,
            IsConfidential = isConfidential,
            LogoUrl = command.LogoUrl,
            HomepageUrl = command.HomepageUrl,
            AccessTokenLifetimeMinutes = command.AccessTokenLifetimeMinutes,
            RefreshTokenLifetimeMinutes = command.RefreshTokenLifetimeMinutes,
            RequireEmailVerified = command.RequireEmailVerified,
            RequirePhoneVerified = command.RequirePhoneVerified
        };

        application.SetRedirectUris(command.RedirectUris ?? []);
        application.SetPostLogoutRedirectUris(command.PostLogoutRedirectUris ?? []);
        application.SetAllowedOrigins(command.AllowedOrigins ?? []);
        application.SetScopes(scopes);
        application.SetGrantTypes(grantTypes);
        application.SetAudiences(command.Audiences ?? []);

        dbContext.Applications.Add(application);
        auditContext.Details = AuditDiff.CaptureState(dbContext.Entry(application));
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Application, EntityId = application.Id, Operation = IndexOperation.Index }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        corsOriginService.InvalidateCache();

        var descriptor = BuildDescriptor(command, clientId, clientSecret, scopes, grantTypes);
        await appManager.CreateAsync(descriptor, cancellationToken);

        var dto = MapToDto(application);
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
            c.GrantTypes, c.Audiences, c.AccessTokenLifetimeMinutes, c.RefreshTokenLifetimeMinutes,
            c.RequireEmailVerified, c.RequirePhoneVerified);

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
