using Auth.Infrastructure;
using FluentAssertions;

namespace Auth.UnitTests;

public class SmtpTwoFactorEmailGatewayTests
{
    private static SmtpOptions DefaultOptions() => new()
    {
        FromEmail = "noreply@test.com",
        FromName = "Test Service"
    };

    [Fact]
    public void BuildEmailMessage_WithValidInputs_SetsCorrectSubject()
    {
        // Arrange
        var options = DefaultOptions();

        // Act
        var message = SmtpTwoFactorEmailGateway.BuildEmailMessage(
            options, "user@example.com", "Your verification code", "<html><body>123456</body></html>");

        // Assert
        message.Subject.Should().Be("Your verification code");
    }

    [Fact]
    public void BuildEmailMessage_WithValidInputs_SetsFromAddress()
    {
        // Arrange
        var options = DefaultOptions();

        // Act
        var message = SmtpTwoFactorEmailGateway.BuildEmailMessage(
            options, "user@example.com", "Subject", "<html></html>");

        // Assert
        message.From.Count.Should().Be(1);
        message.From[0].ToString().Should().Contain("noreply@test.com");
    }

    [Fact]
    public void BuildEmailMessage_WithValidInputs_SetsToAddress()
    {
        // Arrange
        var options = DefaultOptions();

        // Act
        var message = SmtpTwoFactorEmailGateway.BuildEmailMessage(
            options, "user@example.com", "Subject", "<html></html>");

        // Assert
        message.To.Count.Should().Be(1);
        message.To[0].ToString().Should().Contain("user@example.com");
    }

    [Fact]
    public void BuildEmailMessage_WithValidInputs_BodyContainsOtpCode()
    {
        // Arrange
        var options = DefaultOptions();
        const string otp = "123456";

        // Act
        var message = SmtpTwoFactorEmailGateway.BuildEmailMessage(
            options, "user@example.com", "Subject", $"<html><body>{otp}</body></html>");

        // Assert
        var body = message.ToString();
        body.Should().Contain(otp);
    }

    [Fact]
    public void BuildEmailMessage_WithValidInputs_HasTextAlternative()
    {
        // Arrange
        var options = DefaultOptions();

        // Act
        var message = SmtpTwoFactorEmailGateway.BuildEmailMessage(
            options, "user@example.com", "Subject", "<html><body>text</body></html>");

        // Assert
        var body = message.Body.ToString()!;
        body.Should().NotBeNullOrEmpty();
        // Message has multipart body with both HTML and plain text parts
        message.Body.ContentType.MimeType.Should().Be("multipart/alternative");
    }

    [Fact]
    public void StripHtmlTags_RemovesAllTags()
    {
        var result = SmtpTwoFactorEmailGateway.StripHtmlTags("<h1>Hello</h1><p>World</p>");
        result.Should().Be("HelloWorld");
    }

    [Fact]
    public void StripHtmlTags_WithEmptyString_ReturnsEmpty()
    {
        var result = SmtpTwoFactorEmailGateway.StripHtmlTags("");
        result.Should().BeEmpty();
    }
}
