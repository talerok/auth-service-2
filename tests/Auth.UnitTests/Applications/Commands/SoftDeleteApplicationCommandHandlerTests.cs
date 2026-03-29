using Auth.Application;
using Auth.Application.Applications.Commands.SoftDeleteApplication;
using Auth.Application.Messaging.Commands;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Applications.Commands.SoftDeleteApplication;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Applications.Commands;

public sealed class SoftDeleteApplicationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenApplicationExists_SoftDeletesAndReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var application = new Domain.Application { Name = "To Delete", Description = "desc", ClientId = "ac-del", IsActive = true };
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new SoftDeleteApplicationCommandHandler(dbContext, eventBus.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new SoftDeleteApplicationCommand(application.Id),
            CancellationToken.None);

        result.Should().BeTrue();
        var updated = await dbContext.Applications.IgnoreQueryFilters().FirstAsync(x => x.Id == application.Id);
        updated.DeletedAt.Should().NotBeNull();
        eventBus.Verify(x => x.PublishAsync(
            It.Is<IndexEntityRequested>(e => e.EntityType == IndexEntityType.Application && e.EntityId == application.Id && e.Operation == IndexOperation.Delete),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenApplicationDoesNotExist_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new SoftDeleteApplicationCommandHandler(dbContext, eventBus.Object, corsOriginService.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new SoftDeleteApplicationCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeFalse();
    }

}
