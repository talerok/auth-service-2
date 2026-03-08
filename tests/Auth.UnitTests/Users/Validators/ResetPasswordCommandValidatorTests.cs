using Auth.Application;
using Auth.Application.Users.Commands.ResetPassword;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Auth.UnitTests.Users.Validators;

public sealed class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new(
        Options.Create(new PasswordRequirementsOptions()));

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new ResetPasswordCommand(Guid.NewGuid(), "NewPassword1");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyUserId_HasError()
    {
        var command = new ResetPasswordCommand(Guid.Empty, "NewPassword1");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public async Task Validate_WithShortPassword_HasError()
    {
        var command = new ResetPasswordCommand(Guid.NewGuid(), "Ab1");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }
}
