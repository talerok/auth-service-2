using Auth.Application.ServiceAccounts.Commands.UpdateServiceAccount;
using FluentAssertions;

namespace Auth.UnitTests.ServiceAccounts.Validators;

public sealed class UpdateServiceAccountCommandValidatorTests
{
    private readonly UpdateServiceAccountCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new UpdateServiceAccountCommand(Guid.NewGuid(), "Name", "Desc", true);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var command = new UpdateServiceAccountCommand(Guid.Empty, "Name", "Desc", true);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_WithEmptyName_HasError()
    {
        var command = new UpdateServiceAccountCommand(Guid.NewGuid(), "", "Desc", true);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithNameExceedingMaxLength_HasError()
    {
        var command = new UpdateServiceAccountCommand(Guid.NewGuid(), new string('a', 201), "Desc", true);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithDescriptionExceedingMaxLength_HasError()
    {
        var command = new UpdateServiceAccountCommand(Guid.NewGuid(), "Name", new string('a', 501), true);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }
}
