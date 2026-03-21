using MediatR;

namespace Auth.Application.Applications.Commands.SoftDeleteApplication;

public sealed record SoftDeleteApplicationCommand(Guid Id) : IRequest<bool>;
