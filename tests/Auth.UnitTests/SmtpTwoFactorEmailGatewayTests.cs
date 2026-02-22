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
    public void BuildOtpEmailMessage_WithValidInputs_SetsCorrectSubject()
    {
        // Arrange
        var options = DefaultOptions();

        // Act
        var message = SmtpTwoFactorEmailGateway.BuildOtpEmailMessage(options, "user@example.com", "123456");

        // Assert
        message.Subject.Should().Be("Your verification code");
    }

    [Fact]
    public void BuildOtpEmailMessage_WithValidInputs_SetsFromAddress()
    {
        // Arrange
        var options = DefaultOptions();

        // Act
        var message = SmtpTwoFactorEmailGateway.BuildOtpEmailMessage(options, "user@example.com", "123456");

        // Assert
        message.From.Count.Should().Be(1);
        message.From[0].ToString().Should().Contain("noreply@test.com");
    }

    [Fact]
    public void BuildOtpEmailMessage_WithValidInputs_SetsToAddress()
    {
        // Arrange
        var options = DefaultOptions();

        // Act
        var message = SmtpTwoFactorEmailGateway.BuildOtpEmailMessage(options, "user@example.com", "123456");

        // Assert
        message.To.Count.Should().Be(1);
        message.To[0].ToString().Should().Contain("user@example.com");
    }

    [Fact]
    public void BuildOtpEmailMessage_WithValidInputs_BodyContainsOtpCode()
    {
        // Arrange
        var options = DefaultOptions();
        const string otp = "123456";

        // Act
        var message = SmtpTwoFactorEmailGateway.BuildOtpEmailMessage(options, "user@example.com", otp);

        // Assert
        var body = message.ToString();
        body.Should().Contain(otp);
    }

    [Fact]
    public void BuildOtpEmailMessage_WithValidInputs_HasTextAlternative()
    {
        // Arrange
        var options = DefaultOptions();

        // Act
        var message = SmtpTwoFactorEmailGateway.BuildOtpEmailMessage(options, "user@example.com", "123456");

        // Assert
        var body = message.Body.ToString()!;
        body.Should().NotBeNullOrEmpty();
        // Message has multipart body with both HTML and plain text parts
        message.Body.ContentType.MimeType.Should().Be("multipart/alternative");
    }
}
