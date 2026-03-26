using Auth.Application;
using Auth.Application.NotificationTemplates.Commands.UpdateNotificationTemplate;
using Auth.Application.NotificationTemplates.Queries.GetAllNotificationTemplates;
using Auth.Application.NotificationTemplates.Queries.GetNotificationTemplateById;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.NotificationTemplates.Commands.UpdateNotificationTemplate;
using Auth.Infrastructure.NotificationTemplates.Queries.GetAllNotificationTemplates;
using Auth.Infrastructure.NotificationTemplates.Queries.GetNotificationTemplateById;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.NotificationTemplates;

public sealed class NotificationTemplateHandlerTests
{
    // ─── GetAllNotificationTemplatesQueryHandler ─────────────────────────

    [Fact]
    public async Task GetAll_WhenNoTemplates_ReturnsEmptyCollection()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetAllNotificationTemplatesQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllNotificationTemplatesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_WhenTemplatesExist_ReturnsAll()
    {
        await using var dbContext = CreateDbContext();
        dbContext.NotificationTemplates.AddRange(
            new NotificationTemplate { Type = NotificationTemplateType.TwoFactorEmail, Locale = "en-US", Subject = "Email Subject", Body = "Email Body" },
            new NotificationTemplate { Type = NotificationTemplateType.TwoFactorSms, Locale = "en-US", Subject = "Sms Subject", Body = "Sms Body" });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllNotificationTemplatesQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllNotificationTemplatesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.Type).Should().BeEquivalentTo("TwoFactorEmail", "TwoFactorSms");
    }

    [Fact]
    public async Task GetAll_MapsFieldsCorrectly()
    {
        await using var dbContext = CreateDbContext();
        dbContext.NotificationTemplates.Add(
            new NotificationTemplate { Type = NotificationTemplateType.TwoFactorEmail, Locale = "ru-RU", Subject = "Subj", Body = "Body" });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllNotificationTemplatesQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllNotificationTemplatesQuery(), CancellationToken.None);

        var dto = result.Should().ContainSingle().Which;
        dto.Type.Should().Be("TwoFactorEmail");
        dto.Locale.Should().Be("ru-RU");
        dto.Subject.Should().Be("Subj");
        dto.Body.Should().Be("Body");
        dto.Id.Should().NotBeEmpty();
    }

    // ─── GetNotificationTemplateByIdQueryHandler ─────────────────────────

    [Fact]
    public async Task GetById_WhenExists_ReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var template = new NotificationTemplate { Type = NotificationTemplateType.TwoFactorEmail, Locale = "en-US", Subject = "S", Body = "B" };
        dbContext.NotificationTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        var handler = new GetNotificationTemplateByIdQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetNotificationTemplateByIdQuery(template.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
        result.Type.Should().Be("TwoFactorEmail");
        result.Locale.Should().Be("en-US");
        result.Subject.Should().Be("S");
        result.Body.Should().Be("B");
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetNotificationTemplateByIdQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetNotificationTemplateByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    // ─── UpdateNotificationTemplateCommandHandler ────────────────────────

    [Fact]
    public async Task Update_WhenExists_UpdatesAndReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var template = new NotificationTemplate { Type = NotificationTemplateType.TwoFactorEmail, Locale = "en-US", Subject = "Old", Body = "Old Body" };
        dbContext.NotificationTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        var searchIndexService = new Mock<ISearchIndexService>();
        var handler = new UpdateNotificationTemplateCommandHandler(dbContext, searchIndexService.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdateNotificationTemplateCommand(template.Id, "TwoFactorEmail", "ru-RU", "New Subject", "New Body"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
        result.Type.Should().Be("TwoFactorEmail");
        result.Locale.Should().Be("ru-RU");
        result.Subject.Should().Be("New Subject");
        result.Body.Should().Be("New Body");

        var persisted = await dbContext.NotificationTemplates.FindAsync(template.Id);
        persisted!.Subject.Should().Be("New Subject");
        persisted.Body.Should().Be("New Body");
        persisted.Locale.Should().Be("ru-RU");
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndexService = new Mock<ISearchIndexService>();
        var handler = new UpdateNotificationTemplateCommandHandler(dbContext, searchIndexService.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdateNotificationTemplateCommand(Guid.NewGuid(), "TwoFactorEmail", "en-US", "S", "B"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_SetsUpdatedAt()
    {
        await using var dbContext = CreateDbContext();
        var template = new NotificationTemplate { Type = NotificationTemplateType.TwoFactorSms, Locale = "en-US", Subject = "Old", Body = "Old" };
        dbContext.NotificationTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        var originalUpdatedAt = template.UpdatedAt;
        var searchIndexService = new Mock<ISearchIndexService>();
        var handler = new UpdateNotificationTemplateCommandHandler(dbContext, searchIndexService.Object, new Mock<IAuditContext>().Object);

        await Task.Delay(50);
        var result = await handler.Handle(
            new UpdateNotificationTemplateCommand(template.Id, "TwoFactorSms", "en-US", "New", "New"), CancellationToken.None);

        result.Should().NotBeNull();
        var persisted = await dbContext.NotificationTemplates.FindAsync(template.Id);
        persisted!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    // ─── Helper ──────────────────────────────────────────────────────────

}
