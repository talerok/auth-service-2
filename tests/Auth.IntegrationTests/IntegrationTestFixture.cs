using Auth.IntegrationTests.Stubs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Auth.IntegrationTests;

[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollectionDefinition : ICollectionFixture<IntegrationTestFixture>;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("auth_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private string? _adminToken;

    public HttpClient Client => _client ?? throw new InvalidOperationException("Not initialized");

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Set environment variables BEFORE creating the factory.
        // AddInfrastructure in Program.cs eagerly builds NpgsqlDataSource from configuration
        // which runs before ConfigureAppConfiguration overrides are applied.
        SetEnvironmentVariables();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    // Replace search services (OpenSearch not available in tests)
                    services.RemoveAll<ISearchService>();
                    services.AddScoped<ISearchService, StubSearchService>();

                    services.RemoveAll<ISearchIndexService>();
                    services.AddScoped<ISearchIndexService, StubSearchIndexService>();

                    services.RemoveAll<ISearchMaintenanceService>();
                    services.AddScoped<ISearchMaintenanceService, StubSearchMaintenanceService>();

                    // Remove hosted services (OpenSearch init, 2FA background delivery)
                    services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();

                    // Remove OpenSearch health check (not available in tests)
                    services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(
                        options =>
                        {
                            var osCheck = options.Registrations.FirstOrDefault(r => r.Name == "opensearch");
                            if (osCheck is not null)
                                options.Registrations.Remove(osCheck);
                        });
                });
            });

        _client = _factory.CreateClient();

        // Seed runs automatically via Program.cs → SeedAsync()
        // Acquire admin token via real OpenIddict password grant
        _adminToken = await AcquireTokenAsync("admin", "admin");
        SetBearerToken(_adminToken);
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
        ClearEnvironmentVariables();
    }

    private static readonly string[] EnvVarKeys =
    [
        "Integration__PostgreSql__ConnectionString",
        "Integration__OpenSearch__Url",
        "Integration__OpenSearch__EnsureIndicesOnStartup",
        "Integration__OpenSearch__ReindexOnStartup",
        "Integration__Smtp__Enabled",
        "Integration__SmsGateway__Enabled",
        "Integration__TwoFactor__StaticOtpForTesting",
        "Integration__TwoFactor__EncryptionKey",
        "Integration__Cors__AllowedOrigins__0",
        "Integration__Oidc__LoginUrl",
        "Integration__Oidc__ConsentUrl",
    ];

    private void SetEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("Integration__PostgreSql__ConnectionString", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("Integration__OpenSearch__Url", "http://localhost:9200");
        Environment.SetEnvironmentVariable("Integration__OpenSearch__EnsureIndicesOnStartup", "false");
        Environment.SetEnvironmentVariable("Integration__OpenSearch__ReindexOnStartup", "false");
        Environment.SetEnvironmentVariable("Integration__Smtp__Enabled", "false");
        Environment.SetEnvironmentVariable("Integration__SmsGateway__Enabled", "false");
        Environment.SetEnvironmentVariable("Integration__TwoFactor__StaticOtpForTesting", "123456");
        Environment.SetEnvironmentVariable("Integration__TwoFactor__EncryptionKey", "dGVzdC1lbmNyeXB0aW9uLWtleS0xMjM0NTY3OA==");
        Environment.SetEnvironmentVariable("Integration__Cors__AllowedOrigins__0", "http://localhost:3000");
        Environment.SetEnvironmentVariable("Integration__Oidc__LoginUrl", "/auth/login.html");
        Environment.SetEnvironmentVariable("Integration__Oidc__ConsentUrl", "/auth/consent.html");
    }

    private static void ClearEnvironmentVariables()
    {
        foreach (var key in EnvVarKeys)
            Environment.SetEnvironmentVariable(key, null);
    }

    // --- Auth helpers ---

    public async Task<string> AcquireTokenAsync(string username, string password)
    {
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["client_id"] = "frontend-app",
            ["scope"] = "openid profile email phone ws offline_access"
        });

        using var tempClient = _factory!.CreateClient();
        var response = await tempClient.PostAsync("/connect/token", tokenRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "token acquisition should succeed for user '{0}'", username);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("access_token").GetString()!;
    }

    public async Task<(string AccessToken, string RefreshToken)> AcquireTokenWithRefreshAsync(string username, string password)
    {
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["client_id"] = "frontend-app",
            ["scope"] = "openid profile email phone ws offline_access"
        });

        using var tempClient = _factory!.CreateClient();
        var response = await tempClient.PostAsync("/connect/token", tokenRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return (
            json.GetProperty("access_token").GetString()!,
            json.GetProperty("refresh_token").GetString()!
        );
    }

    public void SetBearerToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void SetAdminToken()
    {
        SetBearerToken(_adminToken!);
    }

    public void ClearAuth()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    // --- Data creation helpers ---

    public async Task<UserDto> CreateUserAsync(
        string? username = null,
        string? password = null,
        bool isActive = true,
        bool mustChangePassword = false,
        bool twoFactorEnabled = false)
    {
        username ??= $"user-{Guid.NewGuid():N}";
        password ??= "Test1234!";
        var request = new CreateUserRequest(
            Username: username,
            FullName: $"Test User {username}",
            Email: $"{username}@test.local",
            Password: password,
            IsActive: isActive,
            IsInternalAuthEnabled: true,
            MustChangePassword: mustChangePassword,
            TwoFactorEnabled: twoFactorEnabled);
        var response = await Client.PostAsJsonAsync("/api/users", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "user creation should succeed for '{0}'", username);
        return (await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions))!;
    }

    public async Task<RoleDto> CreateRoleAsync(string? name = null)
    {
        name ??= $"role-{Guid.NewGuid():N}";
        var request = new CreateRoleRequest(name, name, $"Test role {name}");
        var response = await Client.PostAsJsonAsync("/api/roles", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "role creation should succeed for '{0}'", name);
        return (await response.Content.ReadFromJsonAsync<RoleDto>(JsonOptions))!;
    }

    public async Task<WorkspaceDto> CreateWorkspaceAsync(string? name = null)
    {
        name ??= $"ws-{Guid.NewGuid():N}";
        var request = new CreateWorkspaceRequest(name, name, $"Test workspace {name}");
        var response = await Client.PostAsJsonAsync("/api/workspaces", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "workspace creation should succeed for '{0}'", name);
        return (await response.Content.ReadFromJsonAsync<WorkspaceDto>(JsonOptions))!;
    }

    public async Task<PermissionDto> CreatePermissionAsync(string? domain = null, string? code = null)
    {
        domain ??= "custom";
        code ??= $"custom.test.{Guid.NewGuid():N}";
        var request = new CreatePermissionRequest(domain, code, $"Test permission {code}");
        var response = await Client.PostAsJsonAsync("/api/permissions", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "permission creation should succeed for '{0}'", code);
        return (await response.Content.ReadFromJsonAsync<PermissionDto>(JsonOptions))!;
    }

    public async Task<CreateApplicationResponse> CreateApplicationAsync(
        string? name = null, bool isConfidential = false)
    {
        name ??= $"app-{Guid.NewGuid():N}";
        var request = new CreateApplicationRequest(name, $"Test app {name}",
            IsConfidential: isConfidential,
            RedirectUris: ["http://localhost:3000/callback"]);
        var response = await Client.PostAsJsonAsync("/api/applications", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "application creation should succeed for '{0}'", name);
        return (await response.Content.ReadFromJsonAsync<CreateApplicationResponse>(JsonOptions))!;
    }

    public async Task<CreateServiceAccountResponse> CreateServiceAccountAsync(string? name = null)
    {
        name ??= $"sa-{Guid.NewGuid():N}";
        var request = new CreateServiceAccountRequest(name, $"Test SA {name}");
        var response = await Client.PostAsJsonAsync("/api/service-accounts", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "service account creation should succeed for '{0}'", name);
        return (await response.Content.ReadFromJsonAsync<CreateServiceAccountResponse>(JsonOptions))!;
    }

    public async Task<(UserDto User, string Token)> CreateUserWithPermissionsAsync(
        params string[] permissionCodes)
    {
        // Create role
        var role = await CreateRoleAsync();

        // Get matching permissions
        var allPermsResponse = await Client.GetAsync("/api/permissions");
        var allPerms = await allPermsResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<PermissionDto>>(JsonOptions);
        var matchingPerms = allPerms!.Where(p => permissionCodes.Contains(p.Code)).ToList();

        // Assign permissions to role
        var setPermsRequest = new SetPermissionsRequest(matchingPerms);
        await Client.PutAsJsonAsync($"/api/roles/{role.Id}/permissions", setPermsRequest, JsonOptions);

        // Create user
        var password = "Test1234!";
        var user = await CreateUserAsync(password: password);

        // Get system workspace
        var wsResponse = await Client.GetAsync("/api/workspaces");
        var workspaces = await wsResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(JsonOptions);
        var systemWs = workspaces!.First(w => w.Code == "system");

        // Assign user to system workspace with role
        var wsRequest = new SetUserWorkspacesRequest([
            new UserWorkspaceRolesItem(systemWs.Id, [role.Id])
        ]);
        await Client.PutAsJsonAsync($"/api/users/{user.Id}/workspaces", wsRequest, JsonOptions);

        // Get token for the user
        var token = await AcquireTokenAsync(user.Username, password);

        return (user, token);
    }

    // --- Direct DB access ---

    public async Task<T> ExecuteDbAsync<T>(Func<AuthDbContext, Task<T>> action)
    {
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await action(db);
    }

    public async Task ExecuteDbAsync(Func<AuthDbContext, Task> action)
    {
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await action(db);
    }
}
