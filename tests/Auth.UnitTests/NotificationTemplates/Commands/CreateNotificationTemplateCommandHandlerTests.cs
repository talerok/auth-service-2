using Auth.Application;
using Auth.Application.Messaging;
using Auth.Application.NotificationTemplates.Commands.CreateNotificationTemplate;
using Auth.Domain;
using Auth.Infrastructure.NotificationTemplates.Commands.CreateNotificationTemplate;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.NotificationTemplates.Commands;

public sealed class CreateNotificationTemplateCommandHandlerTests
{
    [Fact]
    public async Task Create_ValidInput_ReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var handler = new CreateNotificationTemplateCommandHandler(
            dbContext, new Mock<IEventBus>().Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new CreateNotificationTemplateCommand("TwoFactorEmail", "en-US", "Test Subject", "Test Body"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Type.Should().Be("TwoFactorEmail");
        result.Locale.Should().Be("en-US");
        result.Subject.Should().Be("Test Subject");
        result.Body.Should().Be("Test Body");
        result.Id.Should().NotBeEmpty();

        var persisted = await dbContext.NotificationTemplates.FirstAsync();
        persisted.Type.Should().Be(NotificationTemplateType.TwoFactorEmail);
        persisted.Locale.Should().Be("en-US");
    }

    [Fact]
    public async Task Create_IndexesInOpenSearch()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var handler = new CreateNotificationTemplateCommandHandler(
            dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateNotificationTemplateCommand("TwoFactorSms", "ru-RU", "S", "B"),
            CancellationToken.None);

        eventBus.Verify(x => x.PublishAsync(
            It.IsAny<IIntegrationEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
