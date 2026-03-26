namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class NotificationTemplatesControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    // --- Auth ---

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        fixture.ClearAuth();
        try
        {
            var response = await Client.GetAsync("/api/notification-templates");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // --- GetAll ---

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsTemplates()
    {
        var response = await Client.GetAsync("/api/notification-templates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var templates = await response.Content
            .ReadFromJsonAsync<IReadOnlyCollection<NotificationTemplateDto>>(IntegrationTestFixture.JsonOptions);
        templates.Should().NotBeEmpty();
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingTemplate_Returns200()
    {
        var allResponse = await Client.GetAsync("/api/notification-templates");
        var templates = await allResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<NotificationTemplateDto>>(IntegrationTestFixture.JsonOptions);
        var first = templates!.First();

        var response = await Client.GetAsync($"/api/notification-templates/{first.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var template = await response.Content
            .ReadFromJsonAsync<NotificationTemplateDto>(IntegrationTestFixture.JsonOptions);
        template.Should().NotBeNull();
        template!.Type.Should().NotBeNullOrWhiteSpace();
        template.Locale.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetById_NonexistentId_Returns404()
    {
        var response = await Client.GetAsync($"/api/notification-templates/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Update ---

    [Fact]
    public async Task Update_ExistingTemplate_ReturnsUpdated()
    {
        var allResponse = await Client.GetAsync("/api/notification-templates");
        var templates = await allResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<NotificationTemplateDto>>(IntegrationTestFixture.JsonOptions);
        var first = templates!.First();

        var request = new UpdateNotificationTemplateRequest(
            first.Type, first.Locale, "Updated Subject", "<html><body>Updated OTP: {{otp}}</body></html>");

        var response = await Client.PutAsJsonAsync(
            $"/api/notification-templates/{first.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content
            .ReadFromJsonAsync<NotificationTemplateDto>(IntegrationTestFixture.JsonOptions);
        updated!.Subject.Should().Be("Updated Subject");
    }
}
