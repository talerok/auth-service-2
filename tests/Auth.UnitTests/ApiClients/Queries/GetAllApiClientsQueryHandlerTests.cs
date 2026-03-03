using Auth.Application;
using Auth.Application.ApiClients.Queries.GetAllApiClients;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.ApiClients.Queries.GetAllApiClients;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.ApiClients.Queries;

public sealed class GetAllApiClientsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoApiClients_ReturnsEmptyCollection()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetAllApiClientsQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllApiClientsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenApiClientsExist_ReturnsAll()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ApiClients.AddRange(
            new ApiClient { Name = "A", Description = "d1", ClientId = "ac-1", IsActive = true },
            new ApiClient { Name = "B", Description = "d2", ClientId = "ac-2", IsActive = false });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllApiClientsQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllApiClientsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
