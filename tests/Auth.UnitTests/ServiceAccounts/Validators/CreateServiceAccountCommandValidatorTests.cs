using Auth.Application.ServiceAccounts.Commands.CreateServiceAccount;
using FluentAssertions;

namespace Auth.UnitTests.ServiceAccounts.Validators;

public sealed class CreateServiceAccountCommandValidatorTests
{
    private readonly CreateServiceAccountCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new CreateServiceAccountCommand("My SA", "Description");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyName_HasError(string? name)
    {
        var command = new CreateServiceAccountCommand(name!, "desc");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithNameExceedingMaxLength_HasError()
    {
        var command = new CreateServiceAccountCommand(new string('a', 201), "desc");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithDescriptionExceedingMaxLength_HasError()
    {
        var command = new CreateServiceAccountCommand("SA", new string('a', 501));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Validate_WithEmptyDescription_IsValid()
    {
        var command = new CreateServiceAccountCommand("SA", "");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
