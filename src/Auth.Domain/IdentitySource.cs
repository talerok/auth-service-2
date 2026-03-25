namespace Auth.Domain;

public sealed class IdentitySource : EntityBase
{
    [Auditable] public string Name { get; set; } = string.Empty;
    [Auditable] public string Code { get; set; } = string.Empty;
    [Auditable] public string DisplayName { get; set; } = string.Empty;
    [Auditable] public IdentitySourceType Type { get; init; }
    [Auditable] public bool IsEnabled { get; set; } = true;
    public IdentitySourceOidcConfig? OidcConfig { get; set; }
    public IdentitySourceLdapConfig? LdapConfig { get; set; }
    public ICollection<IdentitySourceLink> Links { get; private set; } = [];
}
