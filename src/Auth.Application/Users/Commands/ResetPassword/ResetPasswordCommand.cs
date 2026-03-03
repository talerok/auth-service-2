using MediatR;

namespace Auth.Application.Users.Commands.ResetPassword;

public sealed record ResetPasswordCommand(Guid UserId, string NewPassword) : IRequest<bool>;
