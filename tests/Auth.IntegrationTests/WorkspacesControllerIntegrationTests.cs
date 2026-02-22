using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Auth.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class WorkspacesControllerIntegrationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Delete_WhenWorkspaceIsSystem_ReturnsBadRequest()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var allWorkspacesResponse = await fixture.Client.GetAsync("/api/workspaces");
        allWorkspacesResponse.EnsureSuccessStatusCode();
        var workspaces = await allWorkspacesResponse.Content.ReadFromJsonAsync<List<WorkspaceDto>>();
        workspaces.Should().NotBeNullOrEmpty();

        var systemWorkspace = workspaces!.First(x => x.IsSystem);
        var deleteResponse = await fixture.Client.DeleteAsync($"/api/workspaces/{systemWorkspace.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await deleteResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("System workspaces cannot be deleted");
    }
}
