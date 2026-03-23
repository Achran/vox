using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Vox.Api.Tests.Fixtures;

namespace Vox.Api.Tests.Integration;

public sealed class VoiceEndpointsIntegrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public VoiceEndpointsIntegrationTests(AuthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -------------------------------------------------------------------------
    // GET /api/voice/token/{channelId}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetVoiceToken_WithValidToken_Returns200WithTokenAndUrl()
    {
        var tokens = await RegisterUserAsync("voiceuser1", "voiceuser1@example.com", "Voice User 1", "Password1");

        var channelId = Guid.NewGuid();
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/voice/token/{channelId}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VoiceTokenResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrEmpty();
        body.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetVoiceToken_WithoutAuthentication_Returns401()
    {
        var channelId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/voice/token/{channelId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetVoiceToken_WithExpiredToken_Returns401()
    {
        var expiredToken = TestJwtHelper.GenerateExpiredToken();

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/voice/token/{Guid.NewGuid()}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetVoiceToken_WithInvalidChannelId_Returns404()
    {
        var tokens = await RegisterUserAsync("voiceuser2", "voiceuser2@example.com", "Voice User 2", "Password1");

        using var msg = new HttpRequestMessage(HttpMethod.Get, "/api/voice/token/not-a-guid");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        // Route constraint {channelId:guid} won't match → 404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<AuthTokensResponse> RegisterUserAsync(
        string userName, string email, string displayName, string password)
    {
        var request = new { UserName = userName, Email = email, DisplayName = displayName, Password = password };
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        return tokens!;
    }

    private sealed record VoiceTokenResponse(string Token, string Url);
}
