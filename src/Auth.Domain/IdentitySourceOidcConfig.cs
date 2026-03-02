namespace Auth.Domain;

public sealed class IdentitySourceOidcConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IdentitySourceId { get; set; }
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
}
