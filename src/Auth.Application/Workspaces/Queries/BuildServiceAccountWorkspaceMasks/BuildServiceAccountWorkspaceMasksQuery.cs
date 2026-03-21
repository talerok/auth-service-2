using MediatR;

namespace Auth.Application.Workspaces.Queries.BuildServiceAccountWorkspaceMasks;

public sealed record BuildServiceAccountWorkspaceMasksQuery(Guid ServiceAccountId) : IRequest<Dictionary<string, Dictionary<string, byte[]>>>;
