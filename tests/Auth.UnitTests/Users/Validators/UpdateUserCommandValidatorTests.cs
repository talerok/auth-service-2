using Auth.Application.Users.Commands.UpdateUser;
using FluentAssertions;

namespace Auth.UnitTests.Users.Validators;

public sealed class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithNegativePasswordMaxAgeDays_HasError()
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), "alice", "Alice", "alice@example.com", null,
            true, PasswordMaxAgeDays: -1);

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
        var command = new UpdateUserCommand(Guid.NewGuid(), "alice", "Alice", "alice@example.com", null,
            true, PasswordMaxAgeDays: days);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
