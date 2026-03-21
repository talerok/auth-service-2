using Auth.Application;
using Auth.Application.ApiClients.Commands.PatchApiClient;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.ApiClients.Commands.PatchApiClient;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;

namespace Auth.UnitTests.ApiClients.Commands;

public sealed class PatchApiClientCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenApiClientExists_PatchesOnlyProvidedFields()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient { Name = "Original", Description = "Orig desc", ClientId = "ac-1", IsActive = true };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new PatchApiClientCommand(apiClient.Id, "Patched", null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Patched");
        result.Description.Should().Be("Orig desc");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenApiClientDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new PatchApiClientCommand(Guid.NewGuid(), "Name", null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenNamePatched_UpdatesOidcDisplayName()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient { Name = "Old", Description = "desc", ClientId = "ac-2", IsActive = true };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        appManager.Setup(x => x.FindByClientIdAsync("ac-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
        var handler = new PatchApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new PatchApiClientCommand(apiClient.Id, "New Name", null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        appManager.Verify(x => x.UpdateAsync(It.IsAny<object>(), It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNameNotPatched_DoesNotTouchOidc()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient { Name = "Name", Description = "desc", ClientId = "ac-3", IsActive = true };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new PatchApiClientCommand(apiClient.Id, null, "new desc", null, null, null, null, null, null, null, null),
            CancellationToken.None);

        appManager.Verify(x => x.FindByClientIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PatchesOAuthFields()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient
        {
            Name = "App", Description = "desc", ClientId = "ac-4",
            IsActive = true, Type = ApiClientType.ServiceAccount
        };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        appManager.Setup(x => x.FindByClientIdAsync("ac-4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
        var handler = new PatchApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new PatchApiClientCommand(apiClient.Id, null, null, null,
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
    public async Task Handle_WhenRedirectUrisPatched_SyncsOidc()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient
        {
            Name = "App", Description = "desc", ClientId = "ac-5", IsActive = true
        };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        appManager.Setup(x => x.FindByClientIdAsync("ac-5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
        var handler = new PatchApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new PatchApiClientCommand(apiClient.Id, null, null, null, null, null, null, null,
                ["https://new.example.com/cb"], null, null),
            CancellationToken.None);

        appManager.Verify(x => x.UpdateAsync(It.IsAny<object>(), It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
