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
}
