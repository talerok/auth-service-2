using Auth.Application;

namespace Auth.Infrastructure;

public sealed class IntegrationOptions
{
    public string EncryptionKey { get; set; } = string.Empty;
    public PostgreSqlOptions PostgreSql { get; set; } = new();
    public OpenSearchOptions OpenSearch { get; set; } = new();

    public TwoFactorOptions TwoFactor { get; set; } = new();
    public PasswordChangeOptions PasswordChange { get; set; } = new();
    public SmtpOptions Smtp { get; set; } = new();
    public SmsGatewayOptions SmsGateway { get; set; } = new();
    public VerificationOptions Verification { get; set; } = new();
    public CorsOptions Cors { get; set; } = new();
    public OidcOptions Oidc { get; set; } = new();
    public PasswordRequirementsOptions PasswordRequirements { get; set; } = new();
    public PasswordExpirationOptions PasswordExpiration { get; set; } = new();
    public RabbitMqOptions RabbitMq { get; set; } = new();
    public RedisOptions Redis { get; set; } = new();
}

public sealed class PostgreSqlOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class OpenSearchOptions
{
    public string Url { get; set; } = string.Empty;
    public string IndexPrefix { get; set; } = "auth";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnsureIndicesOnStartup { get; set; } = true;
    public bool ReindexOnStartup { get; set; }
}

public sealed class TwoFactorOptions
{
    public int OtpLength { get; set; } = 6;
    public int StandardOtpTtlMinutes { get; set; } = 5;
    public int HighRiskOtpTtlMinutes { get; set; } = 3;
    public int MaxAttemptsPerChallenge { get; set; } = 5;
    public int DeliveryTimeoutSeconds { get; set; } = 3;
    public int DeliveryRetryCount { get; set; } = 3;
    public int DeliveryRetryBackoffMilliseconds { get; set; } = 200;
    public int DeliveryPollIntervalMilliseconds { get; set; } = 2000;
    public string StaticOtpForTesting { get; set; } = string.Empty;
}

public sealed class PasswordChangeOptions
{
    public int PasswordChangeTtlMinutes { get; set; } = 15;
}

public sealed class SmtpOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@auth-service";
    public string FromName { get; set; } = "Auth Service";
}

public sealed class CorsOptions
{
    public string AllowedOrigins { get; set; } = string.Empty;

    public string[] GetParsedOrigins() => string.IsNullOrWhiteSpace(AllowedOrigins)
        ? []
        : AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed class SmsGatewayOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 5;
}

public sealed class VerificationOptions
{
    public string EmailBaseUrl { get; set; } = string.Empty;
    public string PhoneBaseUrl { get; set; } = string.Empty;
}

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}

public sealed class RedisOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
}

public sealed class OidcOptions
{
    public string SigningKeyPath { get; set; } = string.Empty;
    public string SigningKeyPassword { get; set; } = string.Empty;
    public string EncryptionKeyPath { get; set; } = string.Empty;
    public string EncryptionKeyPassword { get; set; } = string.Empty;
    public string DevCertDirectory { get; set; } = "./certs";
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 7;
    public int RefreshTokenReuseLeewaySeconds { get; set; } = 30;
    public string LoginUrl { get; set; } = "/auth/login.html";
    public string ConsentUrl { get; set; } = "/auth/consent.html";
}
