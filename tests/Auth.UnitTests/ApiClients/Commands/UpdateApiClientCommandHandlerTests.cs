using Auth.Application;
using Auth.Application.ApiClients.Commands.UpdateApiClient;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.ApiClients.Commands.UpdateApiClient;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;

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
            new UpdateApiClientCommand(apiClient.Id, "New", "New desc", false),
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
            new UpdateApiClientCommand(Guid.NewGuid(), "Name", "Desc", true),
            CancellationToken.None);

        result.Should().BeNull();
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
