using Auth.Application;
using Auth.Application.ApiClients.Commands.CreateApiClient;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.ApiClients.Commands.CreateApiClient;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;

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

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
