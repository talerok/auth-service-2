using Auth.Application.Common;
using Auth.Application.Users.Commands.PatchUser;
using FluentAssertions;

namespace Auth.UnitTests.Users.Validators;

public sealed class PatchUserCommandValidatorTests
{
    private readonly PatchUserCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithNegativePasswordMaxAgeDays_HasError()
    {
        var command = new PatchUserCommand(Guid.NewGuid(), default, default, default, default, default,
            default, default, default, default, default, default, PasswordMaxAgeDays: new Optional<int?>(-1));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PasswordMaxAgeDays");
    }

    [Fact]
    public async Task Validate_WithValidPasswordMaxAgeDays_IsValid()
    {
        var command = new PatchUserCommand(Guid.NewGuid(), default, default, default, default, default,
            default, default, default, default, default, default, PasswordMaxAgeDays: new Optional<int?>(90));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithoutPasswordMaxAgeDays_IsValid()
    {
        var command = new PatchUserCommand(Guid.NewGuid(), default, default, default, default, default,
            default, default, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
