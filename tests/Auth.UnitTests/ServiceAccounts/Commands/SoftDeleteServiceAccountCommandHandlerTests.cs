using Auth.Application;
using Auth.Application.ServiceAccounts.Commands.SoftDeleteServiceAccount;
using Auth.Infrastructure;
using Auth.Infrastructure.ServiceAccounts.Commands.SoftDeleteServiceAccount;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.ServiceAccounts.Commands;

public sealed class SoftDeleteServiceAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenServiceAccountExists_SoftDeletesAndReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "To Delete", Description = "desc", ClientId = "sa-del", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new SoftDeleteServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new SoftDeleteServiceAccountCommand(serviceAccount.Id),
            CancellationToken.None);

        result.Should().BeTrue();
        var updated = await dbContext.ServiceAccounts.IgnoreQueryFilters().FirstAsync(x => x.Id == serviceAccount.Id);
        updated.DeletedAt.Should().NotBeNull();
        searchIndex.Verify(x => x.DeleteServiceAccountAsync(serviceAccount.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceAccountDoesNotExist_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new SoftDeleteServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new SoftDeleteServiceAccountCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeletesOidcApplication()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "SA", Description = "desc", ClientId = "sa-oidc", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var oidcApp = new object();
        appManager.Setup(x => x.FindByClientIdAsync("sa-oidc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(oidcApp);
        var handler = new SoftDeleteServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new SoftDeleteServiceAccountCommand(serviceAccount.Id),
            CancellationToken.None);

        appManager.Verify(x => x.DeleteAsync(oidcApp, It.IsAny<CancellationToken>()), Times.Once);
    }

}
