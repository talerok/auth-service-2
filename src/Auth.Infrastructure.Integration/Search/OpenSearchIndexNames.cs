using Auth.Infrastructure;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Integration.Search;

public sealed class OpenSearchIndexNames
{
    public OpenSearchIndexNames(IOptions<IntegrationOptions> options)
    {
        var prefix = options.Value.OpenSearch.IndexPrefix;
        Users = $"{prefix}-users";
        Roles = $"{prefix}-roles";
        Permissions = $"{prefix}-permissions";
        Workspaces = $"{prefix}-workspaces";
    }

    public string Users { get; }
    public string Roles { get; }
    public string Permissions { get; }
    public string Workspaces { get; }
}
