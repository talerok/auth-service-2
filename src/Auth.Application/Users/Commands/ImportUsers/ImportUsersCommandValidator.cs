using FluentValidation;

namespace Auth.Application.Users.Commands.ImportUsers;

public sealed class ImportUsersCommandValidator : AbstractValidator<ImportUsersCommand>
{
    public ImportUsersCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
    }
}
