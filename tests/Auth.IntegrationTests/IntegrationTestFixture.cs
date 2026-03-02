using Auth.Application;
using Auth.Domain;
using Auth.Infrastructure;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Testcontainers.PostgreSql;

namespace Auth.IntegrationTests;

public sealed record OidcTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("id_token")] string? IdToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);

public sealed record OidcErrorResponse(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("error_description")] string? ErrorDescription,
    [property: JsonPropertyName("mfa_token")] string? MfaToken = null,
    [property: JsonPropertyName("mfa_channel")] string? MfaChannel = null,
    [property: JsonPropertyName("challenge_id")] string? ChallengeId = null);

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false) }
    };

    private static readonly IReadOnlyDictionary<string, string> TestEnvironmentVariables = new Dictionary<string, string>
    {
        ["Integration__OpenSearch__Url"] = "http://localhost:9200",
        ["Integration__OpenSearch__EnsureIndicesOnStartup"] = "false",
        ["Integration__OpenSearch__ReindexOnStartup"] = "false",
        ["Integration__Jwt__Secret"] = "super-secret-key-min-32-characters-long!",
        ["Integration__TwoFactor__StaticOtpForTesting"] = "123456",
        ["Integration__TwoFactor__DeliveryPollIntervalMilliseconds"] = "50"
    };

    private readonly INetwork _network = new NetworkBuilder().Build();
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly Dictionary<string, string?> _originalEnvironment = new(StringComparer.Ordinal);
    private CustomWebApplicationFactory? _factory;

    public HttpClient Client { get; private set; } = null!;

    public IntegrationTestFixture()
    {
        _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithNetwork(_network)
            .WithNetworkAliases("postgres")
            .WithDatabase("auth_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _network.CreateAsync();
        await _postgresContainer.StartAsync();

        var connectionString = _postgresContainer.GetConnectionString();
        ApplyTestEnvironment(connectionString);

        _factory = new CustomWebApplicationFactory(connectionString);
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginResult = await LoginAsync("admin", "admin");
        loginResult.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        RestoreEnvironment();

        await _postgresContainer.DisposeAsync();
        await _network.DisposeAsync();
    }

    public async Task<OidcTokenResponse> LoginAsync(string username, string password)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["client_id"] = "mobile-app",
            ["scope"] = "openid profile email ws offline_access"
        });

        var response = await Client.PostAsync("/connect/token", content);
        response.EnsureSuccessStatusCode();

        var tokens = await response.Content.ReadFromJsonAsync<OidcTokenResponse>(JsonOptions);
        tokens.Should().NotBeNull();
        return tokens!;
    }

    public void SetBearerToken(string accessToken) =>
        Client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

    public void ClearAuth() =>
        Client.DefaultRequestHeaders.Authorization = null;

    public async Task ExecuteAsync(Func<AuthDbContext, Task> action)
    {
        var factory = _factory ?? throw new InvalidOperationException("Factory is not initialized.");
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await action(dbContext);
    }

    public async Task WaitForChallengeDeliveryAsync(Guid challengeId, TimeSpan timeout)
    {
        var factory = _factory ?? throw new InvalidOperationException("Factory is not initialized.");
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var status = await dbContext.TwoFactorChallenges
                .Where(x => x.Id == challengeId)
                .Select(x => x.DeliveryStatus)
                .FirstOrDefaultAsync();

            if (status == TwoFactorChallenge.DeliveryDelivered)
            {
                return;
            }

            if (status is TwoFactorChallenge.DeliveryFailed or TwoFactorChallenge.ProviderUnavailable)
            {
                throw new InvalidOperationException($"Challenge {challengeId} finished with delivery status {status}.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        throw new TimeoutException($"Challenge {challengeId} was not delivered within {timeout}.");
    }

    private sealed class CustomWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Integration:PostgreSql:ConnectionString"] = connectionString,

                    ["Integration:OpenSearch:Url"] = "http://localhost:9200",
                    ["Integration:OpenSearch:EnsureIndicesOnStartup"] = "false",
                    ["Integration:OpenSearch:ReindexOnStartup"] = "false",
                    ["Integration:Jwt:Secret"] = "super-secret-key-min-32-characters-long!",
                    ["Integration:TwoFactor:StaticOtpForTesting"] = "123456",
                    ["Integration:TwoFactor:DeliveryPollIntervalMilliseconds"] = "50"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AuthDbContext>>();
                services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString));

                services.RemoveAll<ISearchIndexService>();
                services.RemoveAll<ISearchService>();
                services.RemoveAll<IPermissionBitCache>();

                services.AddScoped<ISearchIndexService, NullSearchIndexService>();
                services.AddScoped<ISearchService, StubSearchService>();
                services.AddSingleton<IPermissionBitCache, StubPermissionBitCache>();
            });
        }
    }

    private sealed class StubSearchService : ISearchService
    {
        public Task<SearchResponse<UserDto>> SearchUsersAsync(SearchRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new SearchResponse<UserDto>(Array.Empty<UserDto>(), request.Page, request.PageSize, 0));

        public Task<SearchResponse<RoleDto>> SearchRolesAsync(SearchRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new SearchResponse<RoleDto>(Array.Empty<RoleDto>(), request.Page, request.PageSize, 0));

        public Task<SearchResponse<PermissionDto>> SearchPermissionsAsync(SearchRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new SearchResponse<PermissionDto>(Array.Empty<PermissionDto>(), request.Page, request.PageSize, 0));

        public Task<SearchResponse<WorkspaceDto>> SearchWorkspacesAsync(SearchRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new SearchResponse<WorkspaceDto>(Array.Empty<WorkspaceDto>(), request.Page, request.PageSize, 0));
    }

    private sealed class StubPermissionBitCache : IPermissionBitCache
    {
        private readonly IReadOnlyDictionary<string, int> _cache = SystemPermissionCatalog.Permissions
            .ToDictionary(x => x.Code, x => x.Bit, StringComparer.OrdinalIgnoreCase);

        public int GetBitByCode(string code) => _cache[code];

        public bool TryGetBitByCode(string code, out int bit) => _cache.TryGetValue(code, out bit);

        public IReadOnlyDictionary<string, int> Snapshot() => _cache;

        public Task WarmupAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private void ApplyTestEnvironment(string connectionString)
    {
        SetEnvironmentVariable("Integration__PostgreSql__ConnectionString", connectionString);

        foreach (var (key, value) in TestEnvironmentVariables)
        {
            SetEnvironmentVariable(key, value);
        }
    }

    private void RestoreEnvironment()
    {
        foreach (var (key, originalValue) in _originalEnvironment)
        {
            Environment.SetEnvironmentVariable(key, originalValue);
        }

        _originalEnvironment.Clear();
    }

    private void SetEnvironmentVariable(string key, string value)
    {
        if (!_originalEnvironment.ContainsKey(key))
        {
            _originalEnvironment[key] = Environment.GetEnvironmentVariable(key);
        }

        Environment.SetEnvironmentVariable(key, value);
    }
}
