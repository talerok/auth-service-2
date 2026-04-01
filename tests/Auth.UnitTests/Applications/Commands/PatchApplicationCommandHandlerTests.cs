using Auth.Application;
using Auth.Application.Applications.Commands.PatchApplication;
using Auth.Infrastructure;
using Auth.Infrastructure.Applications.Commands.PatchApplication;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Applications.Commands;

public sealed class PatchApplicationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenApplicationExists_PatchesOnlyProvidedFields()
    {
        await using var dbContext = CreateDbContext();
        var application = new Domain.Application { Name = "Original", Description = "Orig desc", ClientId = "ac-1", IsActive = true };
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();

        var handler = new PatchApplicationCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchApplicationCommand(application.Id, "Patched", default, default, default, default, default, default, default, default, default, default, default, default, default, default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Patched");
        result.Description.Should().Be("Orig desc");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenApplicationDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();

        var handler = new PatchApplicationCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchApplicationCommand(Guid.NewGuid(), "Name", default, default, default, default, default, default, default, default, default, default, default, default, default, default, default),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenNamePatched_UpdatesOidcDisplayName()
    {
        await using var dbContext = CreateDbContext();
        var application = new Domain.Application { Name = "Old", Description = "desc", ClientId = "ac-2", IsActive = true };
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        appManager.Setup(x => x.FindByClientIdAsync("ac-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        var handler = new PatchApplicationCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new PatchApplicationCommand(application.Id, "New Name", default, default, default, default, default, default, default, default, default, default, default, default, default, default, default),
            CancellationToken.None);

        appManager.Verify(x => x.UpdateAsync(It.IsAny<object>(), It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNameNotPatched_DoesNotTouchOidc()
    {
        await using var dbContext = CreateDbContext();
        var application = new Domain.Application { Name = "Name", Description = "desc", ClientId = "ac-3", IsActive = true };
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();

        var handler = new PatchApplicationCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new PatchApplicationCommand(application.Id, default, "new desc", default, default, default, default, default, default, default, default, default, default, default, default, default, default),
            CancellationToken.None);

        appManager.Verify(x => x.FindByClientIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PatchesOAuthFields()
    {
        await using var dbContext = CreateDbContext();
        var application = new Domain.Application
        {
            Name = "App", Description = "desc", ClientId = "ac-4",
            IsActive = true
        };
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        appManager.Setup(x => x.FindByClientIdAsync("ac-4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        var handler = new PatchApplicationCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchApplicationCommand(application.Id, default, default, default,
                "https://example.com/logo.png", "https://example.com",
                new List<string> { "https://example.com/cb" }, new List<string> { "https://example.com/logout" }, default, "implicit", default, default, default, default, default, default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.LogoUrl.Should().Be("https://example.com/logo.png");
        result.HomepageUrl.Should().Be("https://example.com");
        result.RedirectUris.Should().ContainSingle("https://example.com/cb");
        result.PostLogoutRedirectUris.Should().ContainSingle("https://example.com/logout");
    }

    [Fact]
    public async Task Handle_WhenRedirectUrisPatched_SyncsOidc()
    {
        await using var dbContext = CreateDbContext();
        var application = new Domain.Application
        {
            Name = "App", Description = "desc", ClientId = "ac-5", IsActive = true
        };
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        appManager.Setup(x => x.FindByClientIdAsync("ac-5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        var handler = new PatchApplicationCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new PatchApplicationCommand(application.Id, default, default, default, default, default,
                new List<string> { "https://new.example.com/cb" }, default, default, default, default, default, default, default, default, default, default),
            CancellationToken.None);

        appManager.Verify(x => x.UpdateAsync(It.IsAny<object>(), It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()), Times.Once);
    }

}
