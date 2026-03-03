using Auth.Application;
using Auth.Application.ApiClients.Commands.RegenerateApiClientSecret;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.ApiClients.Commands.RegenerateApiClientSecret;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;

namespace Auth.UnitTests.ApiClients.Commands;

public sealed class RegenerateApiClientSecretCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenApiClientExists_ReturnsNewSecret()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient { Name = "Client", Description = "desc", ClientId = "ac-regen", IsActive = true };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        appManager.Setup(x => x.FindByClientIdAsync("ac-regen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
        var handler = new RegenerateApiClientSecretCommandHandler(dbContext, appManager.Object);

        var result = await handler.Handle(
            new RegenerateApiClientSecretCommand(apiClient.Id),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ClientSecret.Should().NotBeNullOrEmpty();
        appManager.Verify(x => x.UpdateAsync(It.IsAny<object>(), It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenApiClientDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new RegenerateApiClientSecretCommandHandler(dbContext, appManager.Object);

        var result = await handler.Handle(
            new RegenerateApiClientSecretCommand(Guid.NewGuid()),
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
