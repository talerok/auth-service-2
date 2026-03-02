namespace Auth.Domain;

public sealed class IdentitySourceLdapConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IdentitySourceId { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 389;
    public string BaseDn { get; set; } = string.Empty;
    public string BindDn { get; set; } = string.Empty;
    public string? BindPassword { get; set; }
    public bool UseSsl { get; set; }
    public string SearchFilter { get; set; } = "(uid={username})";
}
