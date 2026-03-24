using Auth.Application.Applications.Queries.GetApplicationById;
using Auth.Infrastructure;
using Auth.Infrastructure.Applications.Queries.GetApplicationById;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Applications.Queries;

public sealed class GetApplicationByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenApplicationExists_ReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var application = new Domain.Application { Name = "Test", Description = "desc", ClientId = "ac-1", IsActive = true };
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync();
        var handler = new GetApplicationByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetApplicationByIdQuery(application.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.ClientId.Should().Be("ac-1");
    }

    [Fact]
    public async Task Handle_ReturnsOAuthFieldsInDto()
    {
        await using var dbContext = CreateDbContext();
        var application = new Domain.Application
        {
            Name = "OAuth", Description = "desc", ClientId = "ac-oauth", IsActive = true,
            IsConfidential = false,
            LogoUrl = "https://example.com/logo.png", HomepageUrl = "https://example.com"
        };
        application.SetRedirectUris(["https://example.com/cb"]);
        application.SetPostLogoutRedirectUris(["https://example.com/logout"]);
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync();
        var handler = new GetApplicationByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetApplicationByIdQuery(application.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsConfidential.Should().BeFalse();
        result.LogoUrl.Should().Be("https://example.com/logo.png");
        result.HomepageUrl.Should().Be("https://example.com");
        result.RedirectUris.Should().ContainSingle("https://example.com/cb");
        result.PostLogoutRedirectUris.Should().ContainSingle("https://example.com/logout");
    }

    [Fact]
    public async Task Handle_WhenApplicationDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetApplicationByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetApplicationByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

}
