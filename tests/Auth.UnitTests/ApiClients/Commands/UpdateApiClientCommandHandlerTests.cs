using Auth.Application;
using Auth.Application.ApiClients.Commands.UpdateApiClient;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.ApiClients.Commands.UpdateApiClient;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using OidcConstants = OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.UnitTests.ApiClients.Commands;

public sealed class UpdateApiClientCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenApiClientExists_UpdatesAndReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient { Name = "Old", Description = "Old desc", ClientId = "ac-123", IsActive = true };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new UpdateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new UpdateApiClientCommand(apiClient.Id, "New", "New desc", false,
                ApiClientType.ServiceAccount, true, null, null, [], [], null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("New");
        result.Description.Should().Be("New desc");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenApiClientDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new UpdateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new UpdateApiClientCommand(Guid.NewGuid(), "Name", "Desc", true,
                ApiClientType.ServiceAccount, true, null, null, [], [], null),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UpdatesOAuthFields()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient { Name = "App", Description = "desc", ClientId = "ac-oauth", IsActive = true };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new UpdateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new UpdateApiClientCommand(apiClient.Id, "OAuth App", "updated", true,
                ApiClientType.OAuthApplication, false,
                "https://example.com/logo.png", "https://example.com",
                ["https://example.com/cb"], ["https://example.com/logout"], "implicit"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Type.Should().Be(ApiClientType.OAuthApplication);
        result.IsConfidential.Should().BeFalse();
        result.LogoUrl.Should().Be("https://example.com/logo.png");
        result.HomepageUrl.Should().Be("https://example.com");
        result.RedirectUris.Should().ContainSingle("https://example.com/cb");
        result.PostLogoutRedirectUris.Should().ContainSingle("https://example.com/logout");
    }

    [Fact]
    public async Task Handle_SyncsOidcApp_WithOAuthPermissions()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient
        {
            Name = "App", Description = "desc", ClientId = "ac-sync",
            IsActive = true, Type = ApiClientType.ServiceAccount
        };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();

        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var oidcApp = new object();
        appManager.Setup(x => x.FindByClientIdAsync("ac-sync", It.IsAny<CancellationToken>()))
            .ReturnsAsync(oidcApp);
        appManager.Setup(x => x.PopulateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), oidcApp, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.UpdateAsync(oidcApp, It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<object, OpenIddictApplicationDescriptor, CancellationToken>((_, d, _) => capturedDescriptor = d)
            .Returns(ValueTask.CompletedTask);

        var handler = new UpdateApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new UpdateApiClientCommand(apiClient.Id, "OAuth App", "desc", true,
                ApiClientType.OAuthApplication, true, null, null,
                ["https://example.com/cb"], [], "explicit"),
            CancellationToken.None);

        capturedDescriptor.Should().NotBeNull();
        capturedDescriptor!.Permissions.Should().Contain(OidcConstants.Permissions.GrantTypes.AuthorizationCode);
        capturedDescriptor.Permissions.Should().Contain(OidcConstants.Permissions.Endpoints.Authorization);
        capturedDescriptor.Requirements.Should().Contain(OidcConstants.Requirements.Features.ProofKeyForCodeExchange);
        capturedDescriptor.ConsentType.Should().Be(OidcConstants.ConsentTypes.Explicit);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
