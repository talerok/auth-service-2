namespace Auth.Domain;

public sealed class IdentitySource : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IdentitySourceType Type { get; set; }
    public bool IsEnabled { get; set; } = true;
    public IdentitySourceOidcConfig? OidcConfig { get; set; }
    public IdentitySourceLdapConfig? LdapConfig { get; set; }
    public ICollection<IdentitySourceLink> Links { get; private set; } = [];
}
