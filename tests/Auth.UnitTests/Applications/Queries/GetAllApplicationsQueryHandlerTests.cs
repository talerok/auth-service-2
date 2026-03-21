using Auth.Application;
using Auth.Application.Applications.Queries.GetAllApplications;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Applications.Queries.GetAllApplications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.Applications.Queries;

public sealed class GetAllApplicationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoApplications_ReturnsEmptyCollection()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetAllApplicationsQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllApplicationsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenApplicationsExist_ReturnsAll()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Applications.AddRange(
            new Domain.Application { Name = "A", Description = "d1", ClientId = "ac-1", IsActive = true },
            new Domain.Application { Name = "B", Description = "d2", ClientId = "ac-2", IsActive = false });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllApplicationsQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllApplicationsQuery(), CancellationToken.None);

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
