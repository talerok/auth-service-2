using Auth.Application.Oidc.Queries.GetApplicationAudiences;
using Auth.Infrastructure;
using Auth.Infrastructure.Oidc.Queries.GetApplicationAudiences;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Applications.Queries;

public sealed class GetApplicationAudiencesQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenApplicationHasAudiences_ReturnsThem()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Applications.Add(new Domain.Application
        {
            Name = "Test", Description = "d", ClientId = "ac-1", IsActive = true,
            Audiences = ["api-orders", "api-billing"]
        });
        await dbContext.SaveChangesAsync();
        var handler = new GetApplicationAudiencesQueryHandler(dbContext);

        var result = await handler.Handle(new GetApplicationAudiencesQuery("ac-1"), CancellationToken.None);

        result.Should().BeEquivalentTo(["api-orders", "api-billing"]);
    }

    [Fact]
    public async Task Handle_WhenApplicationHasNoAudiences_ReturnsEmpty()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Applications.Add(new Domain.Application
        {
            Name = "Test", Description = "d", ClientId = "ac-2", IsActive = true
        });
        await dbContext.SaveChangesAsync();
        var handler = new GetApplicationAudiencesQueryHandler(dbContext);

        var result = await handler.Handle(new GetApplicationAudiencesQuery("ac-2"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenApplicationNotFound_ReturnsEmpty()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetApplicationAudiencesQueryHandler(dbContext);

        var result = await handler.Handle(new GetApplicationAudiencesQuery("ac-unknown"), CancellationToken.None);

        result.Should().BeEmpty();
    }

}
