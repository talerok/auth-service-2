using MediatR;

namespace Auth.Application.Applications.Queries.GetAllApplications;

public sealed record GetAllApplicationsQuery() : IRequest<IReadOnlyCollection<ApplicationDto>>;
