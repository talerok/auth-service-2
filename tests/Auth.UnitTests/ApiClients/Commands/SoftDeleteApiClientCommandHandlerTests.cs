using Auth.Application;
using Auth.Application.ApiClients.Commands.SoftDeleteApiClient;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.ApiClients.Commands.SoftDeleteApiClient;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;

namespace Auth.UnitTests.ApiClients.Commands;

public sealed class SoftDeleteApiClientCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenApiClientExists_SoftDeletesAndReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient { Name = "To Delete", Description = "desc", ClientId = "ac-del", IsActive = true };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new SoftDeleteApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new SoftDeleteApiClientCommand(apiClient.Id),
            CancellationToken.None);

        result.Should().BeTrue();
        var updated = await dbContext.ApiClients.IgnoreQueryFilters().FirstAsync(x => x.Id == apiClient.Id);
        updated.DeletedAt.Should().NotBeNull();
        searchIndex.Verify(x => x.DeleteApiClientAsync(apiClient.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenApiClientDoesNotExist_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new SoftDeleteApiClientCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new SoftDeleteApiClientCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeFalse();
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
