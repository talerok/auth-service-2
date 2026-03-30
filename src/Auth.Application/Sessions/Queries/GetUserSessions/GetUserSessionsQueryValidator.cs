using FluentValidation;

namespace Auth.Application.Sessions.Queries.GetUserSessions;

public sealed class GetUserSessionsQueryValidator : AbstractValidator<GetUserSessionsQuery>
{
    public GetUserSessionsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
