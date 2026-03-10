using MediatR;

namespace Auth.Application.Workspaces.Queries.BuildWorkspaceMasks;

public sealed record BuildWorkspaceMasksQuery(Guid UserId) : IRequest<Dictionary<string, Dictionary<string, byte[]>>>;
