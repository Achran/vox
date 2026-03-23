using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Vox.Api.Tests.Fixtures;

namespace Vox.Api.Tests.Integration;

public sealed class PresenceEndpointsIntegrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PresenceEndpointsIntegrationTests(AuthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -------------------------------------------------------------------------
    // GET /api/servers/{serverId}/online-users
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOnlineServerUsers_WithExistingServer_Returns200()
    {
        var tokens = await RegisterUserAsync("presown1", "presown1@example.com", "Presence Owner 1", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Presence Server 1");

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/servers/{server!.Id}/online-users");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<PresenceUserResponse>>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOnlineServerUsers_WithNonExistentServer_Returns404()
    {
        var tokens = await RegisterUserAsync("presown2", "presown2@example.com", "Presence Owner 2", "Password1");

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/servers/{Guid.NewGuid()}/online-users");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOnlineServerUsers_WithoutAuthentication_Returns401()
    {
        var response = await _client.GetAsync($"/api/servers/{Guid.NewGuid()}/online-users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // GET /api/channels/{channelId}/online-users
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOnlineChannelUsers_WithExistingChannel_Returns200()
    {
        var tokens = await RegisterUserAsync("presch1", "presch1@example.com", "Presence Channel 1", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Channel Presence Server");
        var channel = await CreateChannelAsync(tokens.AccessToken, server!.Id, "presence-channel", "Text");

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/channels/{channel!.Id}/online-users");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<PresenceUserResponse>>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOnlineChannelUsers_WithNonExistentChannel_Returns404()
    {
        var tokens = await RegisterUserAsync("presch2", "presch2@example.com", "Presence Channel 2", "Password1");

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/channels/{Guid.NewGuid()}/online-users");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOnlineChannelUsers_WithoutAuthentication_Returns401()
    {
        var response = await _client.GetAsync($"/api/channels/{Guid.NewGuid()}/online-users");

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

    private async Task<ServerResponse?> CreateServerAsync(string accessToken, string name)
    {
        var request = new { Name = name, Description = (string?)null };

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/servers");
        msg.Content = JsonContent.Create(request);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(msg);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServerResponse>();
    }

    private async Task<ChannelResponse?> CreateChannelAsync(
        string accessToken, Guid serverId, string name, string type)
    {
        var request = new { Name = name, Type = type };

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/servers/{serverId}/channels");
        msg.Content = JsonContent.Create(request);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(msg);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChannelResponse>();
    }

    private sealed record ServerResponse(
        Guid Id,
        string Name,
        string? Description,
        string? IconUrl,
        Guid OwnerId,
        DateTime CreatedAt);

    private sealed record ChannelResponse(
        Guid Id,
        string Name,
        string Type,
        Guid ServerId,
        DateTime CreatedAt);

    private sealed record PresenceUserResponse(
        string UserId,
        string Status,
        string? DisplayName);
}
