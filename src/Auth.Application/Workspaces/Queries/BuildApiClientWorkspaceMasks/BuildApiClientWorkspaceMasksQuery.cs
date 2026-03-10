using MediatR;

namespace Auth.Application.Workspaces.Queries.BuildApiClientWorkspaceMasks;

public sealed record BuildApiClientWorkspaceMasksQuery(Guid ApiClientId) : IRequest<Dictionary<string, Dictionary<string, byte[]>>>;
