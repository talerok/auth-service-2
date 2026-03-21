using MediatR;

namespace Auth.Application.Applications.Queries.GetApplicationById;

public sealed record GetApplicationByIdQuery(Guid Id) : IRequest<ApplicationDto?>;
