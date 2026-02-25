using System.Net;
using FluentAssertions;

namespace Auth.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class RolesControllerIntegrationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task SetPermissions_WithAdminToken_UpdatesRolePermissions()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createRoleResponse = await fixture.Client.PostAsJsonAsync("/api/roles", new
        {
            name = $"role_{suffix}",
            description = "integration test role"
        });
        createRoleResponse.EnsureSuccessStatusCode();
        var role = await createRoleResponse.Content.ReadFromJsonAsync<RoleDto>();
        role.Should().NotBeNull();

        var permissionsResponse = await fixture.Client.GetAsync("/api/permissions");
        permissionsResponse.EnsureSuccessStatusCode();
        var permissions = await permissionsResponse.Content.ReadFromJsonAsync<List<PermissionDto>>();
        permissions.Should().NotBeNullOrEmpty();
        var selectedPermission = permissions!.First();

        var updatePermissionsResponse = await fixture.Client.PutAsJsonAsync($"/api/roles/{role!.Id}/permissions", new
        {
            permissions = new[]
            {
                new
                {
                    id = selectedPermission.Id,
                    bit = selectedPermission.Bit,
                    code = selectedPermission.Code,
                    description = selectedPermission.Description,
                    isSystem = selectedPermission.IsSystem
                }
            }
        });

        updatePermissionsResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetPermissions_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var response = await fixture.Client.GetAsync($"/api/roles/{Guid.NewGuid()}/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPermissions_WhenRoleHasNoPermissions_ReturnsEmptyArray()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createRoleResponse = await fixture.Client.PostAsJsonAsync("/api/roles", new
        {
            name = $"role_{suffix}",
            description = "integration test role"
        });
        createRoleResponse.EnsureSuccessStatusCode();
        var role = await createRoleResponse.Content.ReadFromJsonAsync<RoleDto>();
        role.Should().NotBeNull();

        var response = await fixture.Client.GetAsync($"/api/roles/{role!.Id}/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PermissionDto>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissions_AfterSettingPermissions_ReturnsAssignedPermissions()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createRoleResponse = await fixture.Client.PostAsJsonAsync("/api/roles", new
        {
            name = $"role_{suffix}",
            description = "integration test role"
        });
        createRoleResponse.EnsureSuccessStatusCode();
        var role = await createRoleResponse.Content.ReadFromJsonAsync<RoleDto>();
        role.Should().NotBeNull();

        var permissionsResponse = await fixture.Client.GetAsync("/api/permissions");
        permissionsResponse.EnsureSuccessStatusCode();
        var allPermissions = await permissionsResponse.Content.ReadFromJsonAsync<List<PermissionDto>>();
        allPermissions.Should().NotBeNullOrEmpty();
        var selectedPermission = allPermissions!.First();

        await fixture.Client.PutAsJsonAsync($"/api/roles/{role!.Id}/permissions", new
        {
            permissions = new[]
            {
                new
                {
                    id = selectedPermission.Id,
                    bit = selectedPermission.Bit,
                    code = selectedPermission.Code,
                    description = selectedPermission.Description,
                    isSystem = selectedPermission.IsSystem
                }
            }
        });

        var response = await fixture.Client.GetAsync($"/api/roles/{role.Id}/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PermissionDto>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result!.Single().Id.Should().Be(selectedPermission.Id);
        result.Single().Code.Should().Be(selectedPermission.Code);
    }
}
