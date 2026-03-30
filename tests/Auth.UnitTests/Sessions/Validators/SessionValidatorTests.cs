using Auth.Application.Sessions.Commands.CreateSession;
using Auth.Application.Sessions.Commands.RevokeOwnSession;
using Auth.Application.Sessions.Commands.RevokeSession;
using Auth.Application.Sessions.Commands.RevokeUserSessions;
using Auth.Application.Sessions.Commands.TouchSession;
using Auth.Application.Sessions.Queries.GetUserSessions;
using FluentAssertions;

namespace Auth.UnitTests.Sessions.Validators;

public sealed class CreateSessionCommandValidatorTests
{
    private readonly CreateSessionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new CreateSessionCommand(Guid.NewGuid(), "web-app", "pwd", "127.0.0.1", "UA");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyUserId_HasError()
    {
        var command = new CreateSessionCommand(Guid.Empty, "web-app", "pwd", "127.0.0.1", "UA");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyAuthMethod_HasError(string? authMethod)
    {
        var command = new CreateSessionCommand(Guid.NewGuid(), "web-app", authMethod!, "127.0.0.1", "UA");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AuthMethod");
    }

    [Fact]
    public async Task Validate_WithAuthMethodExceedingMaxLength_HasError()
    {
        var command = new CreateSessionCommand(Guid.NewGuid(), "web-app", new string('x', 33), "127.0.0.1", "UA");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AuthMethod");
    }
}

public sealed class TouchSessionCommandValidatorTests
{
    private readonly TouchSessionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var result = await _validator.ValidateAsync(new TouchSessionCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptySessionId_HasError()
    {
        var result = await _validator.ValidateAsync(new TouchSessionCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SessionId");
    }

    [Fact]
    public async Task Validate_WithEmptyUserId_HasError()
    {
        var result = await _validator.ValidateAsync(new TouchSessionCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}

public sealed class RevokeSessionCommandValidatorTests
{
    private readonly RevokeSessionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var result = await _validator.ValidateAsync(
            new RevokeSessionCommand(Guid.NewGuid(), "admin"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptySessionId_HasError()
    {
        var result = await _validator.ValidateAsync(
            new RevokeSessionCommand(Guid.Empty, "admin"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SessionId");
    }

    [Fact]
    public async Task Validate_WithEmptyReason_HasError()
    {
        var result = await _validator.ValidateAsync(
            new RevokeSessionCommand(Guid.NewGuid(), ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public async Task Validate_WithReasonExceedingMaxLength_HasError()
    {
        var result = await _validator.ValidateAsync(
            new RevokeSessionCommand(Guid.NewGuid(), new string('x', 101)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }
}

public sealed class RevokeOwnSessionCommandValidatorTests
{
    private readonly RevokeOwnSessionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var result = await _validator.ValidateAsync(
            new RevokeOwnSessionCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptySessionId_HasError()
    {
        var result = await _validator.ValidateAsync(
            new RevokeOwnSessionCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SessionId");
    }

    [Fact]
    public async Task Validate_WithEmptyUserId_HasError()
    {
        var result = await _validator.ValidateAsync(
            new RevokeOwnSessionCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}

public sealed class RevokeUserSessionsCommandValidatorTests
{
    private readonly RevokeUserSessionsCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var result = await _validator.ValidateAsync(
            new RevokeUserSessionsCommand(Guid.NewGuid(), "revoke-all"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyUserId_HasError()
    {
        var result = await _validator.ValidateAsync(
            new RevokeUserSessionsCommand(Guid.Empty, "revoke-all"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public async Task Validate_WithEmptyReason_HasError()
    {
        var result = await _validator.ValidateAsync(
            new RevokeUserSessionsCommand(Guid.NewGuid(), ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public async Task Validate_WithReasonExceedingMaxLength_HasError()
    {
        var result = await _validator.ValidateAsync(
            new RevokeUserSessionsCommand(Guid.NewGuid(), new string('x', 101)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }
}

public sealed class GetUserSessionsQueryValidatorTests
{
    private readonly GetUserSessionsQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidQuery_IsValid()
    {
        var result = await _validator.ValidateAsync(new GetUserSessionsQuery(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyUserId_HasError()
    {
        var result = await _validator.ValidateAsync(new GetUserSessionsQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}

