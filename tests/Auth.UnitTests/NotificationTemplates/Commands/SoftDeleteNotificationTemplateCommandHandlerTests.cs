using Auth.Application;
using Auth.Application.NotificationTemplates.Commands.SoftDeleteNotificationTemplate;
using Auth.Domain;
using Auth.Infrastructure.NotificationTemplates.Commands.SoftDeleteNotificationTemplate;
using FluentAssertions;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.NotificationTemplates.Commands;

public sealed class SoftDeleteNotificationTemplateCommandHandlerTests
{
    [Fact]
    public async Task Delete_WhenNotFound_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var handler = new SoftDeleteNotificationTemplateCommandHandler(
            dbContext, new Mock<ISearchIndexService>().Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new SoftDeleteNotificationTemplateCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WhenFound_SoftDeletesAndReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var template = new NotificationTemplate
        {
            Type = NotificationTemplateType.PhoneVerification,
            Locale = "en-US",
            Subject = "Verify",
            Body = "Your code is {code}"
        };
        dbContext.NotificationTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        var handler = new SoftDeleteNotificationTemplateCommandHandler(
            dbContext, new Mock<ISearchIndexService>().Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new SoftDeleteNotificationTemplateCommand(template.Id),
            CancellationToken.None);

        result.Should().BeTrue();

        var persisted = await dbContext.NotificationTemplates.FindAsync(template.Id);
        persisted.Should().NotBeNull();
        persisted!.DeletedAt.Should().NotBeNull();
        persisted.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Delete_DeletesFromSearchIndex()
    {
        await using var dbContext = CreateDbContext();
        var template = new NotificationTemplate
        {
            Type = NotificationTemplateType.TwoFactorEmail,
            Locale = "ru-RU",
            Subject = "S",
            Body = "B"
        };
        dbContext.NotificationTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        var searchService = new Mock<ISearchIndexService>();
        var handler = new SoftDeleteNotificationTemplateCommandHandler(
            dbContext, searchService.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new SoftDeleteNotificationTemplateCommand(template.Id),
            CancellationToken.None);

        searchService.Verify(x => x.DeleteNotificationTemplateAsync(
            template.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
