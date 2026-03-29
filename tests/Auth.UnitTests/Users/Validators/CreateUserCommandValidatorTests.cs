using Auth.Application;
using Auth.Application.Users.Commands.CreateUser;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Auth.UnitTests.Users.Validators;

public sealed class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new(
        Options.Create(new PasswordRequirementsOptions()));

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new CreateUserCommand("alice", "Alice", "alice@example.com", "StrongPass1");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyUsername_HasError(string? username)
    {
        var command = new CreateUserCommand(username!, "Full Name", "email@test.com", "Password1");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public async Task Validate_WithInvalidEmail_HasError(string email)
    {
        var command = new CreateUserCommand("alice", "Alice", email, "Password1");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WithShortPassword_HasError()
    {
        var command = new CreateUserCommand("alice", "Alice", "alice@example.com", "Ab1");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_WithNegativePasswordMaxAgeDays_HasError()
    {
        var command = new CreateUserCommand("alice", "Alice", "alice@example.com", "StrongPass1", PasswordMaxAgeDays: -1);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PasswordMaxAgeDays");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(90)]
    public async Task Validate_WithValidPasswordMaxAgeDays_IsValid(int? days)
    {
        var command = new CreateUserCommand("alice", "Alice", "alice@example.com", "StrongPass1", PasswordMaxAgeDays: days);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
