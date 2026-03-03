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
            new PatchApiClientCommand(apiClient.Id, "Patched", null, null),
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
            new PatchApiClientCommand(Guid.NewGuid(), "Name", null, null),
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
            new PatchApiClientCommand(apiClient.Id, "New Name", null, null),
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
            new PatchApiClientCommand(apiClient.Id, null, "new desc", null),
            CancellationToken.None);

        appManager.Verify(x => x.FindByClientIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
