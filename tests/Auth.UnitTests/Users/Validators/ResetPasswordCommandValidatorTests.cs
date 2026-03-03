using Auth.Application.Users.Commands.ResetPassword;
using FluentAssertions;

namespace Auth.UnitTests.Users.Validators;

public sealed class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new ResetPasswordCommand(Guid.NewGuid(), "newPassword123");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyUserId_HasError()
    {
        var command = new ResetPasswordCommand(Guid.Empty, "newPassword123");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public async Task Validate_WithShortPassword_HasError()
    {
        var command = new ResetPasswordCommand(Guid.NewGuid(), "12345");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }
}
