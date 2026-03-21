using Auth.Application;
using Auth.Application.ApiClients.Commands.CreateApiClient;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.ApiClients.Commands.CreateApiClient;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using OidcConstants = OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.UnitTests.ApiClients.Commands;

public sealed class CreateApiClientCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesApiClientAndReturnsResponse()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new CreateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new CreateApiClientCommand("My Client", "Some description", true),
            CancellationToken.None);

        result.Client.Name.Should().Be("My Client");
        result.Client.Description.Should().Be("Some description");
        result.Client.IsActive.Should().BeTrue();
        result.Client.ClientId.Should().StartWith("ac-");
        result.ClientSecret.Should().NotBeNullOrEmpty();

        var saved = await dbContext.ApiClients.FirstAsync();
        saved.Name.Should().Be("My Client");
    }

    [Fact]
    public async Task Handle_RegistersOpenIddictApplication()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new CreateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new CreateApiClientCommand("My Client", "desc"),
            CancellationToken.None);

        appManager.Verify(x => x.CreateAsync(
            It.Is<OpenIddictApplicationDescriptor>(d => d.DisplayName == "My Client"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IndexesApiClientInSearch()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new CreateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new CreateApiClientCommand("My Client", "desc"),
            CancellationToken.None);

        searchIndex.Verify(x => x.IndexApiClientAsync(
            It.Is<ApiClientDto>(d => d.Name == "My Client"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OAuthApp_SetsAuthorizationCodePermissions()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var handler = new CreateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new CreateApiClientCommand("OAuth App", "desc",
                Type: ApiClientType.OAuthApplication,
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
    public async Task Handle_OAuthApp_SetsRedirectUris()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var handler = new CreateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new CreateApiClientCommand("OAuth App", "desc",
                Type: ApiClientType.OAuthApplication,
                RedirectUris: ["https://example.com/callback", "https://example.com/cb2"],
                PostLogoutRedirectUris: ["https://example.com/logout"]),
            CancellationToken.None);

        capturedDescriptor.Should().NotBeNull();
        capturedDescriptor!.RedirectUris.Should().HaveCount(2);
        capturedDescriptor.RedirectUris.Should().Contain(new Uri("https://example.com/callback"));
        capturedDescriptor.PostLogoutRedirectUris.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_OAuthApp_PublicClient_NoSecret()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var handler = new CreateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new CreateApiClientCommand("SPA App", "desc",
                Type: ApiClientType.OAuthApplication,
                IsConfidential: false,
                RedirectUris: ["https://spa.example.com/callback"]),
            CancellationToken.None);

        result.ClientSecret.Should().BeNull();
        capturedDescriptor!.ClientType.Should().Be(OidcConstants.ClientTypes.Public);
        capturedDescriptor.ClientSecret.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OAuthApp_ImplicitConsent_SetsConsentType()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var handler = new CreateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new CreateApiClientCommand("OAuth App", "desc",
                Type: ApiClientType.OAuthApplication,
                RedirectUris: ["https://example.com/cb"],
                ConsentType: "implicit"),
            CancellationToken.None);

        capturedDescriptor!.ConsentType.Should().Be(OidcConstants.ConsentTypes.Implicit);
    }

    [Fact]
    public async Task Handle_ServiceAccount_SetsClientCredentialsPermissions()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var handler = new CreateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new CreateApiClientCommand("SA", "desc",
                Type: ApiClientType.ServiceAccount),
            CancellationToken.None);

        capturedDescriptor.Should().NotBeNull();
        capturedDescriptor!.Permissions.Should().Contain(OidcConstants.Permissions.GrantTypes.ClientCredentials);
        capturedDescriptor.Permissions.Should().NotContain(OidcConstants.Permissions.GrantTypes.AuthorizationCode);
        capturedDescriptor.Permissions.Should().NotContain(OidcConstants.Permissions.Endpoints.Authorization);
    }

    [Fact]
    public async Task Handle_OAuthApp_StoresNewFieldsInEntity()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new CreateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new CreateApiClientCommand("OAuth App", "desc",
                Type: ApiClientType.OAuthApplication,
                IsConfidential: false,
                LogoUrl: "https://example.com/logo.png",
                HomepageUrl: "https://example.com",
                RedirectUris: ["https://example.com/cb"],
                PostLogoutRedirectUris: ["https://example.com/logout"]),
            CancellationToken.None);

        result.Client.Type.Should().Be(ApiClientType.OAuthApplication);
        result.Client.IsConfidential.Should().BeFalse();
        result.Client.LogoUrl.Should().Be("https://example.com/logo.png");
        result.Client.HomepageUrl.Should().Be("https://example.com");
        result.Client.RedirectUris.Should().HaveCount(1);
        result.Client.PostLogoutRedirectUris.Should().HaveCount(1);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
