using Auth.Application;
using Auth.Application.NotificationTemplates.Commands.UpdateNotificationTemplate;
using Auth.Application.NotificationTemplates.Queries.GetAllNotificationTemplates;
using Auth.Application.NotificationTemplates.Queries.GetNotificationTemplateByChannel;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.NotificationTemplates.Commands.UpdateNotificationTemplate;
using Auth.Infrastructure.NotificationTemplates.Queries.GetAllNotificationTemplates;
using Auth.Infrastructure.NotificationTemplates.Queries.GetNotificationTemplateByChannel;
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
            new NotificationTemplate { Channel = TwoFactorChannel.Email, Subject = "Email Subject", Body = "Email Body" },
            new NotificationTemplate { Channel = TwoFactorChannel.Sms, Subject = "Sms Subject", Body = "Sms Body" });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllNotificationTemplatesQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllNotificationTemplatesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.Channel).Should().BeEquivalentTo("email", "sms");
    }

    [Fact]
    public async Task GetAll_MapsChannelToLowerInvariant()
    {
        await using var dbContext = CreateDbContext();
        dbContext.NotificationTemplates.Add(
            new NotificationTemplate { Channel = TwoFactorChannel.Email, Subject = "Subj", Body = "Body" });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllNotificationTemplatesQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllNotificationTemplatesQuery(), CancellationToken.None);

        var dto = result.Should().ContainSingle().Which;
        dto.Channel.Should().Be("email");
        dto.Subject.Should().Be("Subj");
        dto.Body.Should().Be("Body");
        dto.Id.Should().NotBeEmpty();
    }

    // ─── GetNotificationTemplateByChannelQueryHandler ────────────────────

    [Fact]
    public async Task GetByChannel_WhenValidChannelAndExists_ReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var template = new NotificationTemplate { Channel = TwoFactorChannel.Email, Subject = "S", Body = "B" };
        dbContext.NotificationTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        var handler = new GetNotificationTemplateByChannelQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetNotificationTemplateByChannelQuery("Email"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
        result.Channel.Should().Be("email");
        result.Subject.Should().Be("S");
        result.Body.Should().Be("B");
    }

    [Fact]
    public async Task GetByChannel_WhenInvalidChannel_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetNotificationTemplateByChannelQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetNotificationTemplateByChannelQuery("InvalidChannel"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByChannel_WhenValidChannelButNotFound_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetNotificationTemplateByChannelQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetNotificationTemplateByChannelQuery("Sms"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByChannel_IsCaseInsensitive()
    {
        await using var dbContext = CreateDbContext();
        dbContext.NotificationTemplates.Add(
            new NotificationTemplate { Channel = TwoFactorChannel.Sms, Subject = "S", Body = "B" });
        await dbContext.SaveChangesAsync();
        var handler = new GetNotificationTemplateByChannelQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetNotificationTemplateByChannelQuery("sms"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Channel.Should().Be("sms");
    }

    // ─── UpdateNotificationTemplateCommandHandler ────────────────────────

    [Fact]
    public async Task Update_WhenValidChannelAndExists_UpdatesAndReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var template = new NotificationTemplate { Channel = TwoFactorChannel.Email, Subject = "Old", Body = "Old Body" };
        dbContext.NotificationTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        var handler = new UpdateNotificationTemplateCommandHandler(dbContext, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdateNotificationTemplateCommand("Email", "New Subject", "New Body"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
        result.Channel.Should().Be("email");
        result.Subject.Should().Be("New Subject");
        result.Body.Should().Be("New Body");

        var persisted = await dbContext.NotificationTemplates.FindAsync(template.Id);
        persisted!.Subject.Should().Be("New Subject");
        persisted.Body.Should().Be("New Body");
    }

    [Fact]
    public async Task Update_WhenInvalidChannel_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new UpdateNotificationTemplateCommandHandler(dbContext, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdateNotificationTemplateCommand("Pigeon", "S", "B"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_WhenValidChannelButNotFound_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new UpdateNotificationTemplateCommandHandler(dbContext, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdateNotificationTemplateCommand("Email", "S", "B"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_SetsUpdatedAt()
    {
        await using var dbContext = CreateDbContext();
        var template = new NotificationTemplate { Channel = TwoFactorChannel.Sms, Subject = "Old", Body = "Old" };
        dbContext.NotificationTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        var originalUpdatedAt = template.UpdatedAt;
        var handler = new UpdateNotificationTemplateCommandHandler(dbContext, new Mock<IAuditContext>().Object);

        await Task.Delay(50);
        var result = await handler.Handle(
            new UpdateNotificationTemplateCommand("Sms", "New", "New"), CancellationToken.None);

        result.Should().NotBeNull();
        var persisted = await dbContext.NotificationTemplates.FindAsync(template.Id);
        persisted!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    // ─── Helper ──────────────────────────────────────────────────────────

}
