using Auth.Application;
using Auth.Application.Applications.Commands.CreateApplication;
using Auth.Infrastructure;
using Auth.Infrastructure.Applications.Commands.CreateApplication;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using OidcConstants = OpenIddict.Abstractions.OpenIddictConstants;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Applications.Commands;

public sealed class CreateApplicationCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesApplicationAndReturnsResponse()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var corsOriginService = new Mock<ICorsOriginService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new CreateApplicationCommand("My Client", "Some description", true),
            CancellationToken.None);

        result.Application.Name.Should().Be("My Client");
        result.Application.Description.Should().Be("Some description");
        result.Application.IsActive.Should().BeTrue();
        result.Application.ClientId.Should().StartWith("ac-");
        result.ClientSecret.Should().NotBeNullOrEmpty();

        var saved = await dbContext.Applications.FirstAsync();
        saved.Name.Should().Be("My Client");
    }

    [Fact]
    public async Task Handle_RegistersOpenIddictApplication()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateApplicationCommand("My Client", "desc"),
            CancellationToken.None);

        appManager.Verify(x => x.CreateAsync(
            It.Is<OpenIddictApplicationDescriptor>(d => d.DisplayName == "My Client"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IndexesApplicationInSearch()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateApplicationCommand("My Client", "desc"),
            CancellationToken.None);

        searchIndex.Verify(x => x.IndexApplicationAsync(
            It.Is<ApplicationDto>(d => d.Name == "My Client"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SetsAuthorizationCodePermissions()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateApplicationCommand("OAuth App", "desc",
                RedirectUris: ["https://example.com/callback"]),
            CancellationToken.None);

        capturedDescriptor.Should().NotBeNull();
        capturedDescriptor!.Permissions.Should().Contain(OidcConstants.Permissions.Endpoints.Authorization);
        capturedDescriptor.Permissions.Should().Contain(OidcConstants.Permissions.Endpoints.Token);
        capturedDescriptor.Permissions.Should().Contain(OidcConstants.Permissions.GrantTypes.AuthorizationCode);
        capturedDescriptor.Permissions.Should().Contain(OidcConstants.Permissions.GrantTypes.RefreshToken);
        capturedDescriptor.Permissions.Should().Contain(OidcConstants.Permissions.ResponseTypes.Code);
        capturedDescriptor.Requirements.Should().Contain(OidcConstants.Requirements.Features.ProofKeyForCodeExchange);
    }

    [Fact]
    public async Task Handle_SetsRedirectUris()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateApplicationCommand("OAuth App", "desc",
                RedirectUris: ["https://example.com/callback", "https://example.com/cb2"],
                PostLogoutRedirectUris: ["https://example.com/logout"]),
            CancellationToken.None);

        capturedDescriptor.Should().NotBeNull();
        capturedDescriptor!.RedirectUris.Should().HaveCount(2);
        capturedDescriptor.RedirectUris.Should().Contain(new Uri("https://example.com/callback"));
        capturedDescriptor.PostLogoutRedirectUris.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_PublicClient_NoSecret()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new CreateApplicationCommand("SPA App", "desc",
                IsConfidential: false,
                RedirectUris: ["https://spa.example.com/callback"]),
            CancellationToken.None);

        result.ClientSecret.Should().BeNull();
        capturedDescriptor!.ClientType.Should().Be(OidcConstants.ClientTypes.Public);
        capturedDescriptor.ClientSecret.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ImplicitConsent_SetsConsentType()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateApplicationCommand("OAuth App", "desc",
                RedirectUris: ["https://example.com/cb"],
                ConsentType: "implicit"),
            CancellationToken.None);

        capturedDescriptor!.ConsentType.Should().Be(OidcConstants.ConsentTypes.Implicit);
    }

    [Fact]
    public async Task Handle_StoresNewFieldsInEntity()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new CreateApplicationCommand("OAuth App", "desc",
                IsConfidential: false,
                LogoUrl: "https://example.com/logo.png",
                HomepageUrl: "https://example.com",
                RedirectUris: ["https://example.com/cb"],
                PostLogoutRedirectUris: ["https://example.com/logout"]),
            CancellationToken.None);

        result.Application.IsConfidential.Should().BeFalse();
        result.Application.LogoUrl.Should().Be("https://example.com/logo.png");
        result.Application.HomepageUrl.Should().Be("https://example.com");
        result.Application.RedirectUris.Should().HaveCount(1);
        result.Application.PostLogoutRedirectUris.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithCustomGrantTypes_SetsCorrectPermissions()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateApplicationCommand("M2M App", "desc",
                GrantTypes: ["client_credentials"]),
            CancellationToken.None);

        capturedDescriptor.Should().NotBeNull();
        capturedDescriptor!.Permissions.Should().Contain(OidcConstants.Permissions.GrantTypes.ClientCredentials);
        capturedDescriptor.Permissions.Should().NotContain(OidcConstants.Permissions.GrantTypes.AuthorizationCode);
        capturedDescriptor.Requirements.Should().NotContain(OidcConstants.Requirements.Features.ProofKeyForCodeExchange);
    }

    [Fact]
    public async Task Handle_WithDefaultGrantTypes_SetsAuthorizationCodePermissions()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateApplicationCommand("Web App", "desc"),
            CancellationToken.None);

        capturedDescriptor.Should().NotBeNull();
        capturedDescriptor!.Permissions.Should().Contain(OidcConstants.Permissions.GrantTypes.AuthorizationCode);
        capturedDescriptor.Permissions.Should().Contain(OidcConstants.Permissions.GrantTypes.RefreshToken);
        capturedDescriptor.Requirements.Should().Contain(OidcConstants.Requirements.Features.ProofKeyForCodeExchange);
    }

    [Fact]
    public async Task Handle_WithTokenLifetimes_SetsDescriptorSettings()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateApplicationCommand("App", "desc",
                AccessTokenLifetimeMinutes: 30,
                RefreshTokenLifetimeMinutes: 1440),
            CancellationToken.None);

        capturedDescriptor.Should().NotBeNull();
        capturedDescriptor!.Settings.Should().ContainKey("oidc:token_lifetimes:access_token");
        capturedDescriptor.Settings.Should().ContainKey("oidc:token_lifetimes:refresh_token");
    }

    [Fact]
    public async Task Handle_WithNullTokenLifetimes_NoSettingsInDescriptor()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateApplicationCommand("App", "desc"),
            CancellationToken.None);

        capturedDescriptor.Should().NotBeNull();
        capturedDescriptor!.Settings.Should().NotContainKey("oidc:token_lifetimes:access_token");
        capturedDescriptor.Settings.Should().NotContainKey("oidc:token_lifetimes:refresh_token");
    }

    [Fact]
    public async Task Handle_StoresGrantTypesInEntity()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new CreateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new CreateApplicationCommand("App", "desc",
                GrantTypes: ["client_credentials", "refresh_token"],
                AccessTokenLifetimeMinutes: 60,
                RefreshTokenLifetimeMinutes: 10080),
            CancellationToken.None);

        result.Application.GrantTypes.Should().BeEquivalentTo(["client_credentials", "refresh_token"]);
        result.Application.AccessTokenLifetimeMinutes.Should().Be(60);
        result.Application.RefreshTokenLifetimeMinutes.Should().Be(10080);

        var saved = await dbContext.Applications.FirstAsync();
        saved.GrantTypes.Should().BeEquivalentTo(["client_credentials", "refresh_token"]);
        saved.AccessTokenLifetimeMinutes.Should().Be(60);
    }

}
