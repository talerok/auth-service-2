using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.ServiceAccounts.Commands.PatchServiceAccount;
using Auth.Infrastructure;
using Auth.Infrastructure.ServiceAccounts.Commands.PatchServiceAccount;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.ServiceAccounts.Commands;

public sealed class PatchServiceAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenServiceAccountExists_PatchesOnlyProvidedFields()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "Original", Description = "Orig desc", ClientId = "sa-1", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchServiceAccountCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchServiceAccountCommand(serviceAccount.Id, "Patched", default, default, default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Patched");
        result.Description.Should().Be("Orig desc");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenServiceAccountDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchServiceAccountCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchServiceAccountCommand(Guid.NewGuid(), "Name", default, default, default, default),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenNamePatched_UpdatesOidcDisplayName()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "Old", Description = "desc", ClientId = "sa-2", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        appManager.Setup(x => x.FindByClientIdAsync("sa-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
        appManager.Setup(x => x.PopulateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var handler = new PatchServiceAccountCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new PatchServiceAccountCommand(serviceAccount.Id, "New Name", default, default, default, default),
            CancellationToken.None);

        appManager.Verify(x => x.UpdateAsync(It.IsAny<object>(), It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNameNotPatched_DoesNotTouchOidc()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "Name", Description = "desc", ClientId = "sa-3", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchServiceAccountCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new PatchServiceAccountCommand(serviceAccount.Id, default, "new desc", default, default, default),
            CancellationToken.None);

        appManager.Verify(x => x.FindByClientIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PatchesIsActive()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "SA", Description = "desc", ClientId = "sa-4", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchServiceAccountCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchServiceAccountCommand(serviceAccount.Id, default, default, false, default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_IndexesServiceAccountInSearch()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "SA", Description = "desc", ClientId = "sa-5", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchServiceAccountCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new PatchServiceAccountCommand(serviceAccount.Id, default, "new desc", default, default, default),
            CancellationToken.None);

        eventBus.Verify(x => x.PublishAsync(
            It.Is<IndexEntityRequested>(e => e.EntityType == IndexEntityType.ServiceAccount && e.Operation == IndexOperation.Index),
            It.IsAny<CancellationToken>()), Times.Once);
    }

}
