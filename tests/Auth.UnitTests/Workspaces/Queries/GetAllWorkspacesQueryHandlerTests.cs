using Auth.Application;
using Auth.Application.Workspaces.Queries.GetAllWorkspaces;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Workspaces.Queries.GetAllWorkspaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Workspaces.Queries;

public sealed class GetAllWorkspacesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllWorkspaces()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Workspaces.AddRange(
            new Workspace { Name = "Alpha", Code = "alpha", Description = "First" },
            new Workspace { Name = "Beta", Code = "beta", Description = "Second" });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllWorkspacesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("Alpha", "Beta");
    }

}
