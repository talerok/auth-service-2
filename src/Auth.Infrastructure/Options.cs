namespace Auth.Infrastructure;

public sealed class IntegrationOptions
{
    public PostgreSqlOptions PostgreSql { get; set; } = new();
    public OpenSearchOptions OpenSearch { get; set; } = new();

    public JwtOptions Jwt { get; set; } = new();
    public TwoFactorOptions TwoFactor { get; set; } = new();
    public PasswordChangeOptions PasswordChange { get; set; } = new();
    public SmtpOptions Smtp { get; set; } = new();
    public SmsGatewayOptions SmsGateway { get; set; } = new();
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


public sealed class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "auth-service";
    public string Audience { get; set; } = "auth-service-clients";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public sealed class TwoFactorOptions
{
    public int OtpLength { get; set; } = 6;
    public string EncryptionKey { get; set; } = string.Empty;
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

public sealed class SmsGatewayOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 5;
}
