using Auth.Application.ServiceAccounts.Queries.GetServiceAccountById;
using Auth.Infrastructure;
using Auth.Infrastructure.ServiceAccounts.Queries.GetServiceAccountById;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.ServiceAccounts.Queries;

public sealed class GetServiceAccountByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenServiceAccountExists_ReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "Test", Description = "desc", ClientId = "sa-1", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var handler = new GetServiceAccountByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetServiceAccountByIdQuery(serviceAccount.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.ClientId.Should().Be("sa-1");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenServiceAccountDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetServiceAccountByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetServiceAccountByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

}
