using Auth.Application;
using Auth.Application.Applications.Commands.RegenerateApplicationSecret;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Applications.Commands.RegenerateApplicationSecret;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Applications.Commands;

public sealed class RegenerateApplicationSecretCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenApplicationExists_ReturnsNewSecret()
    {
        await using var dbContext = CreateDbContext();
        var application = new Domain.Application { Name = "Client", Description = "desc", ClientId = "ac-regen", IsActive = true };
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        appManager.Setup(x => x.FindByClientIdAsync("ac-regen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
        var handler = new RegenerateApplicationSecretCommandHandler(dbContext, appManager.Object);

        var result = await handler.Handle(
            new RegenerateApplicationSecretCommand(application.Id),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ClientSecret.Should().NotBeNullOrEmpty();
        appManager.Verify(x => x.UpdateAsync(It.IsAny<object>(), It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenApplicationDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new RegenerateApplicationSecretCommandHandler(dbContext, appManager.Object);

        var result = await handler.Handle(
            new RegenerateApplicationSecretCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeNull();
    }

}
