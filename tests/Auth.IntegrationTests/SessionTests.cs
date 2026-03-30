using Auth.Application.Sessions;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class SessionTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    // ─── Account: GET /api/account/sessions ───────────────────────

    [Fact]
    public async Task GetOwnSessions_ReturnsCurrentSession()
    {
        var (_, token) = await fixture.CreateUserWithPermissionsAsync("system.users.view");
        fixture.SetBearerToken(token);
        try
        {
            var response = await Client.GetAsync("/api/account/sessions");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var sessions = await response.Content
                .ReadFromJsonAsync<List<UserSessionResponse>>(IntegrationTestFixture.JsonOptions);
            sessions.Should().NotBeEmpty();
            sessions.Should().Contain(s => s.IsCurrent);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task GetOwnSessions_WithoutAuth_Returns401()
    {
        fixture.ClearAuth();
        try
        {
            var response = await Client.GetAsync("/api/account/sessions");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // ─── Account: DELETE /api/account/sessions/{id} ───────────────

    [Fact]
    public async Task RevokeOwnSession_RevokesSuccessfully()
    {
        var (user, token) = await fixture.CreateUserWithPermissionsAsync("system.users.view");
        fixture.SetBearerToken(token);
        try
        {
            var sessionsResponse = await Client.GetAsync("/api/account/sessions");
            var sessions = await sessionsResponse.Content
                .ReadFromJsonAsync<List<UserSessionResponse>>(IntegrationTestFixture.JsonOptions);
            var currentSession = sessions!.First(s => s.IsCurrent);

            var revokeResponse = await Client.DeleteAsync($"/api/account/sessions/{currentSession.Id}");

            revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var isRevoked = await fixture.ExecuteDbAsync(async db =>
            {
                var session = await db.UserSessions.FirstAsync(s => s.Id == currentSession.Id);
                return session.IsRevoked;
            });
            isRevoked.Should().BeTrue();
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task RevokeOwnSession_OtherUsersSession_Returns404()
    {
        var (user1, token1) = await fixture.CreateUserWithPermissionsAsync("system.users.view");
        var (user2, _) = await fixture.CreateUserWithPermissionsAsync("system.users.view");

        var user2SessionId = await fixture.ExecuteDbAsync(async db =>
        {
            var session = await db.UserSessions.FirstAsync(s => s.UserId == user2.Id);
            return session.Id;
        });

        fixture.SetBearerToken(token1);
        try
        {
            var response = await Client.DeleteAsync($"/api/account/sessions/{user2SessionId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // ─── Account: DELETE /api/account/sessions ────────────────────

    [Fact]
    public async Task RevokeAllOwnSessions_RevokesAll()
    {
        var (user, token) = await fixture.CreateUserWithPermissionsAsync("system.users.view");
        fixture.SetBearerToken(token);
        try
        {
            var response = await Client.DeleteAsync("/api/account/sessions");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var allRevoked = await fixture.ExecuteDbAsync(async db =>
            {
                return await db.UserSessions
                    .Where(s => s.UserId == user.Id)
                    .AllAsync(s => s.IsRevoked);
            });
            allRevoked.Should().BeTrue();
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // ─── Token flow: session created on login ─────────────────────

    [Fact]
    public async Task PasswordGrant_CreatesSession()
    {
        var (user, _) = await fixture.CreateUserWithPermissionsAsync("system.users.view");

        var sessionExists = await fixture.ExecuteDbAsync(async db =>
        {
            return await db.UserSessions.AnyAsync(s => s.UserId == user.Id);
        });
        sessionExists.Should().BeTrue();
    }

    // ─── Admin: GET /api/users/{userId}/sessions ──────────────────

    [Fact]
    public async Task AdminGetUserSessions_WithPermission_ReturnsSessions()
    {
        var (targetUser, _) = await fixture.CreateUserWithPermissionsAsync("system.users.view");

        var (_, adminToken) = await fixture.CreateUserWithPermissionsAsync("system.sessions.view");
        fixture.SetBearerToken(adminToken);
        try
        {
            var response = await Client.GetAsync($"/api/users/{targetUser.Id}/sessions");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var sessions = await response.Content
                .ReadFromJsonAsync<List<UserSessionResponse>>(IntegrationTestFixture.JsonOptions);
            sessions.Should().NotBeEmpty();
            sessions.Should().OnlyContain(s => s.UserId == targetUser.Id);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task AdminGetUserSessions_WithoutPermission_Returns403()
    {
        var (_, token) = await fixture.CreateUserWithPermissionsAsync("system.users.view");
        fixture.SetBearerToken(token);
        try
        {
            var response = await Client.GetAsync($"/api/users/{Guid.NewGuid()}/sessions");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // ─── Admin: DELETE /api/users/{userId}/sessions/{id} ──────────

    [Fact]
    public async Task AdminRevokeSession_WithPermission_RevokesSession()
    {
        var (targetUser, _) = await fixture.CreateUserWithPermissionsAsync("system.users.view");

        var targetSessionId = await fixture.ExecuteDbAsync(async db =>
        {
            var session = await db.UserSessions.FirstAsync(s => s.UserId == targetUser.Id);
            return session.Id;
        });

        var (_, adminToken) = await fixture.CreateUserWithPermissionsAsync("system.sessions.revoke");
        fixture.SetBearerToken(adminToken);
        try
        {
            var response = await Client.DeleteAsync(
                $"/api/users/{targetUser.Id}/sessions/{targetSessionId}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var isRevoked = await fixture.ExecuteDbAsync(async db =>
            {
                var session = await db.UserSessions.FirstAsync(s => s.Id == targetSessionId);
                return session.IsRevoked;
            });
            isRevoked.Should().BeTrue();
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task AdminRevokeSession_NotFound_Returns404()
    {
        var (_, adminToken) = await fixture.CreateUserWithPermissionsAsync("system.sessions.revoke");
        fixture.SetBearerToken(adminToken);
        try
        {
            var response = await Client.DeleteAsync(
                $"/api/users/{Guid.NewGuid()}/sessions/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // ─── Admin: DELETE /api/users/{userId}/sessions ─────────────

    [Fact]
    public async Task AdminRevokeAllUserSessions_WithPermission_RevokesAll()
    {
        var (targetUser, _) = await fixture.CreateUserWithPermissionsAsync("system.users.view");

        var (_, adminToken) = await fixture.CreateUserWithPermissionsAsync("system.sessions.revoke-all");
        fixture.SetBearerToken(adminToken);
        try
        {
            var response = await Client.DeleteAsync($"/api/users/{targetUser.Id}/sessions");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var allRevoked = await fixture.ExecuteDbAsync(async db =>
            {
                return await db.UserSessions
                    .Where(s => s.UserId == targetUser.Id)
                    .AllAsync(s => s.IsRevoked);
            });
            allRevoked.Should().BeTrue();
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task AdminRevokeAllUserSessions_WithoutPermission_Returns403()
    {
        var (_, token) = await fixture.CreateUserWithPermissionsAsync("system.sessions.revoke");
        fixture.SetBearerToken(token);
        try
        {
            var response = await Client.DeleteAsync($"/api/users/{Guid.NewGuid()}/sessions");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // ─── Refresh token: TouchSession ──────────────────────────────

    [Fact]
    public async Task RefreshGrant_UpdatesSessionLastActivityAt()
    {
        var user = await fixture.CreateUserAsync(password: "Test1234!");
        var (_, refreshToken) = await fixture.AcquireTokenWithRefreshAsync(user.Username, "Test1234!");

        var initialLastActivity = await fixture.ExecuteDbAsync(async db =>
        {
            var session = await db.UserSessions.FirstAsync(s => s.UserId == user.Id);
            return session.LastActivityAt;
        });

        // Small delay to ensure timestamp difference
        await Task.Delay(100);

        var response = await ExchangeRefreshTokenAsync(refreshToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedLastActivity = await fixture.ExecuteDbAsync(async db =>
        {
            var session = await db.UserSessions.FirstAsync(s => s.UserId == user.Id);
            return session.LastActivityAt;
        });

        updatedLastActivity.Should().BeAfter(initialLastActivity);
    }

    [Fact]
    public async Task RefreshGrant_WhenSessionRevoked_ReturnsError()
    {
        var user = await fixture.CreateUserAsync(password: "Test1234!");
        var (_, refreshToken) = await fixture.AcquireTokenWithRefreshAsync(user.Username, "Test1234!");

        // Revoke the session directly in DB
        await fixture.ExecuteDbAsync(async db =>
        {
            var session = await db.UserSessions.FirstAsync(s => s.UserId == user.Id);
            session.Revoke("test");
            await db.SaveChangesAsync();
        });

        var response = await ExchangeRefreshTokenAsync(refreshToken);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    // ─── Concurrent sessions ──────────────────────────────────────

    [Fact]
    public async Task MultipleLogins_CreateMultipleSessions()
    {
        var user = await fixture.CreateUserAsync(password: "Test1234!");

        // Two logins produce two sessions
        await fixture.AcquireTokenWithRefreshAsync(user.Username, "Test1234!");
        await fixture.AcquireTokenWithRefreshAsync(user.Username, "Test1234!");

        var sessionCount = await fixture.ExecuteDbAsync(async db =>
            await db.UserSessions.CountAsync(s => s.UserId == user.Id && !s.IsRevoked));

        sessionCount.Should().BeGreaterThanOrEqualTo(2);
    }


    // ─── Helpers ──────────────────────────────────────────────────

    private async Task<HttpResponseMessage> ExchangeRefreshTokenAsync(string refreshToken)
    {
        var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = "system-app"
        });
        return await Client.PostAsync("/connect/token", request);
    }
}
