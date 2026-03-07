using Auth.Application;
using Auth.Application.Users.Commands.ImportUsers;
using FluentAssertions;

namespace Auth.UnitTests.Users.Validators;

public sealed class ImportUsersCommandValidatorTests
{
    private readonly ImportUsersCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new ImportUsersCommand(
        [
            new ImportUserItem("john", "John", "john@example.com", null, true, true, false, false, null, null, null)
        ]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyItems_HasError()
    {
        var command = new ImportUsersCommand([]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }
}
