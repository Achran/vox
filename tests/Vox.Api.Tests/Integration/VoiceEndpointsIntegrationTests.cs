using System.IdentityModel.Tokens.Jwt;
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

    [Fact]
    public async Task GetVoiceToken_ReturnsDecodableJwt()
    {
        var tokens = await RegisterUserAsync("voiceuser3", "voiceuser3@example.com", "Voice User 3", "Password1");

        var channelId = Guid.NewGuid();
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/voice/token/{channelId}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<VoiceTokenResponse>();
        body.Should().NotBeNull();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(body!.Token).Should().BeTrue();

        var jwt = handler.ReadJwtToken(body.Token);
        jwt.Claims.Should().Contain(c => c.Type == "sub");
        jwt.Claims.Should().Contain(c => c.Type == "name");
        jwt.Payload.Should().ContainKey("video");
    }

    [Fact]
    public async Task GetVoiceToken_ContainsCorrectRoomName()
    {
        var tokens = await RegisterUserAsync("voiceuser4", "voiceuser4@example.com", "Voice User 4", "Password1");

        var channelId = Guid.NewGuid();
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/voice/token/{channelId}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<VoiceTokenResponse>();
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(body!.Token);

        var videoGrant = jwt.Payload["video"] as System.Text.Json.JsonElement?;
        videoGrant.Should().NotBeNull();
        videoGrant!.Value.GetProperty("room").GetString().Should().Be($"voice-{channelId}");
    }

    [Fact]
    public async Task GetVoiceToken_DifferentChannels_ReturnDifferentTokens()
    {
        var tokens = await RegisterUserAsync("voiceuser5", "voiceuser5@example.com", "Voice User 5", "Password1");

        var channelId1 = Guid.NewGuid();
        var channelId2 = Guid.NewGuid();

        using var msg1 = new HttpRequestMessage(HttpMethod.Get, $"/api/voice/token/{channelId1}");
        msg1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response1 = await _client.SendAsync(msg1);

        using var msg2 = new HttpRequestMessage(HttpMethod.Get, $"/api/voice/token/{channelId2}");
        msg2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response2 = await _client.SendAsync(msg2);

        var body1 = await response1.Content.ReadFromJsonAsync<VoiceTokenResponse>();
        var body2 = await response2.Content.ReadFromJsonAsync<VoiceTokenResponse>();

        // Tokens should be different (e.g., due to different channel or unique claims)
        body1!.Token.Should().NotBe(body2!.Token);

        // Additionally, verify that each token's video.room claim matches its channel ID
        var handler = new JwtSecurityTokenHandler();
        var jwt1 = handler.ReadJwtToken(body1.Token);
        var jwt2 = handler.ReadJwtToken(body2.Token);

        var videoGrant1 = jwt1.Payload["video"] as System.Text.Json.JsonElement?;
        var videoGrant2 = jwt2.Payload["video"] as System.Text.Json.JsonElement?;

        videoGrant1.Should().NotBeNull();
        videoGrant2.Should().NotBeNull();

        videoGrant1!.Value.GetProperty("room").GetString().Should().Be($"voice-{channelId1}");
        videoGrant2!.Value.GetProperty("room").GetString().Should().Be($"voice-{channelId2}");
    }

    [Fact]
    public async Task GetVoiceToken_WithInvalidSignature_Returns401()
    {
        var invalidToken = TestJwtHelper.GenerateTokenWithCustomSecret(
            "user-id", "test@example.com", "testuser", "Test User",
            "wrong-secret-key-that-is-at-least-32-chars-long!");

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/voice/token/{Guid.NewGuid()}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
