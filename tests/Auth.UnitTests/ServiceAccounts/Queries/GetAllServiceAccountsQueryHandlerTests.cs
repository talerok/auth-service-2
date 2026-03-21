using Auth.Application.ServiceAccounts.Queries.GetAllServiceAccounts;
using Auth.Infrastructure;
using Auth.Infrastructure.ServiceAccounts.Queries.GetAllServiceAccounts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.ServiceAccounts.Queries;

public sealed class GetAllServiceAccountsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoServiceAccounts_ReturnsEmptyCollection()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetAllServiceAccountsQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllServiceAccountsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenServiceAccountsExist_ReturnsAll()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ServiceAccounts.AddRange(
            new Domain.ServiceAccount { Name = "A", Description = "d1", ClientId = "sa-1", IsActive = true },
            new Domain.ServiceAccount { Name = "B", Description = "d2", ClientId = "sa-2", IsActive = false });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllServiceAccountsQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllServiceAccountsQuery(), CancellationToken.None);

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
