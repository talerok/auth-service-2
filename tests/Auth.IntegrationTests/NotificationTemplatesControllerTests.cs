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

    // --- GetByChannel ---

    [Fact]
    public async Task GetByChannel_Email_Returns200()
    {
        var response = await Client.GetAsync("/api/notification-templates/Email");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var template = await response.Content
            .ReadFromJsonAsync<NotificationTemplateDto>(IntegrationTestFixture.JsonOptions);
        template.Should().NotBeNull();
        template!.Channel.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetByChannel_NonexistentChannel_Returns404()
    {
        var response = await Client.GetAsync("/api/notification-templates/Unknown");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Update ---

    [Fact]
    public async Task Update_EmailTemplate_ReturnsUpdated()
    {
        var request = new UpdateNotificationTemplateRequest(
            "Updated Subject", "<html><body>Updated OTP: {{otp}}</body></html>");

        var response = await Client.PutAsJsonAsync(
            "/api/notification-templates/Email", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content
            .ReadFromJsonAsync<NotificationTemplateDto>(IntegrationTestFixture.JsonOptions);
        updated!.Subject.Should().Be("Updated Subject");
    }
}
