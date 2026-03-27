using Auth.Application;
using Auth.Application.NotificationTemplates.Commands.PatchNotificationTemplate;
using Auth.Domain;
using Auth.Infrastructure.NotificationTemplates.Commands.PatchNotificationTemplate;
using FluentAssertions;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.NotificationTemplates.Commands;

public sealed class PatchNotificationTemplateCommandHandlerTests
{
    [Fact]
    public async Task Patch_WhenNotFound_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new PatchNotificationTemplateCommandHandler(
            dbContext, new Mock<ISearchIndexService>().Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchNotificationTemplateCommand(Guid.NewGuid(), default, default, "New Subject", default),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Patch_WhenFound_UpdatesOnlyProvidedFields()
    {
        await using var dbContext = CreateDbContext();
        var template = new NotificationTemplate
        {
            Type = NotificationTemplateType.TwoFactorEmail,
            Locale = "en-US",
            Subject = "Original Subject",
            Body = "Original Body"
        };
        dbContext.NotificationTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        var handler = new PatchNotificationTemplateCommandHandler(
            dbContext, new Mock<ISearchIndexService>().Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchNotificationTemplateCommand(template.Id, default, default, "Patched Subject", default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
        result.Subject.Should().Be("Patched Subject");
        result.Locale.Should().Be("en-US");
        result.Body.Should().Be("Original Body");
        result.Type.Should().Be("TwoFactorEmail");

        var persisted = await dbContext.NotificationTemplates.FindAsync(template.Id);
        persisted!.Subject.Should().Be("Patched Subject");
        persisted.Locale.Should().Be("en-US");
        persisted.Body.Should().Be("Original Body");
        persisted.Type.Should().Be(NotificationTemplateType.TwoFactorEmail);
    }

    [Fact]
    public async Task Patch_IndexesInOpenSearch()
    {
        await using var dbContext = CreateDbContext();
        var template = new NotificationTemplate
        {
            Type = NotificationTemplateType.EmailVerification,
            Locale = "de-DE",
            Subject = "Betreff",
            Body = "Inhalt"
        };
        dbContext.NotificationTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        var searchService = new Mock<ISearchIndexService>();
        var handler = new PatchNotificationTemplateCommandHandler(
            dbContext, searchService.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new PatchNotificationTemplateCommand(template.Id, default, default, "Neuer Betreff", default),
            CancellationToken.None);

        searchService.Verify(x => x.IndexNotificationTemplateAsync(
            It.Is<NotificationTemplateDto>(d =>
                d.Id == template.Id &&
                d.Subject == "Neuer Betreff" &&
                d.Type == "EmailVerification"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
