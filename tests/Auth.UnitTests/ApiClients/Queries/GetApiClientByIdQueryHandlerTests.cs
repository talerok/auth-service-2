using Auth.Application;
using Auth.Application.ApiClients.Queries.GetApiClientById;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.ApiClients.Queries.GetApiClientById;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.ApiClients.Queries;

public sealed class GetApiClientByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenApiClientExists_ReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient { Name = "Test", Description = "desc", ClientId = "ac-1", IsActive = true };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();
        var handler = new GetApiClientByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetApiClientByIdQuery(apiClient.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.ClientId.Should().Be("ac-1");
    }

    [Fact]
    public async Task Handle_ReturnsOAuthFieldsInDto()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient
        {
            Name = "OAuth", Description = "desc", ClientId = "ac-oauth", IsActive = true,
            Type = ApiClientType.OAuthApplication, IsConfidential = false,
            LogoUrl = "https://example.com/logo.png", HomepageUrl = "https://example.com",
            RedirectUris = ["https://example.com/cb"],
            PostLogoutRedirectUris = ["https://example.com/logout"]
        };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();
        var handler = new GetApiClientByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetApiClientByIdQuery(apiClient.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Type.Should().Be(ApiClientType.OAuthApplication);
        result.IsConfidential.Should().BeFalse();
        result.LogoUrl.Should().Be("https://example.com/logo.png");
        result.HomepageUrl.Should().Be("https://example.com");
        result.RedirectUris.Should().ContainSingle("https://example.com/cb");
        result.PostLogoutRedirectUris.Should().ContainSingle("https://example.com/logout");
    }

    [Fact]
    public async Task Handle_WhenApiClientDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetApiClientByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetApiClientByIdQuery(Guid.NewGuid()), CancellationToken.None);

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
