using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Vox.Api.Tests.Fixtures;

namespace Vox.Api.Tests.Integration;

public sealed class MessageEndpointsIntegrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MessageEndpointsIntegrationTests(AuthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -------------------------------------------------------------------------
    // GET /api/channels/{channelId}/messages
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetChannelMessages_WithAuthentication_Returns200()
    {
        var tokens = await RegisterUserAsync("msguser1", "msguser1@example.com", "Msg User 1", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Msg Server 1");
        var channel = await CreateChannelAsync(tokens.AccessToken, server!.Id, "msg-channel-1");

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/channels/{channel!.Id}/messages");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>();
        messages.Should().NotBeNull();
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task GetChannelMessages_WithoutAuthentication_Returns401()
    {
        var channelId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/channels/{channelId}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetChannelMessages_WithPageSizeParameter_Returns200()
    {
        var tokens = await RegisterUserAsync("msgpage1", "msgpage1@example.com", "Msg Page 1", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Msg Server Page");
        var channel = await CreateChannelAsync(tokens.AccessToken, server!.Id, "msg-page-channel");

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/channels/{channel!.Id}/messages?pageSize=10");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetChannelMessages_WithBeforeParameter_Returns200()
    {
        var tokens = await RegisterUserAsync("msgbefore1", "msgbefore1@example.com", "Msg Before 1", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Msg Server Before");
        var channel = await CreateChannelAsync(tokens.AccessToken, server!.Id, "msg-before-channel");

        var before = DateTimeOffset.UtcNow.ToString("o");

        using var msg = new HttpRequestMessage(HttpMethod.Get,
            $"/api/channels/{channel!.Id}/messages?before={Uri.EscapeDataString(before)}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetChannelMessages_WithNonExistentChannel_ReturnsEmptyList()
    {
        var tokens = await RegisterUserAsync("msgnoexist1", "msgnoexist1@example.com", "Msg No Exist", "Password1");
        var nonExistentChannelId = Guid.NewGuid();

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/channels/{nonExistentChannelId}/messages");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>();
        messages.Should().NotBeNull();
        messages.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Rate Limiting
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetChannelMessages_ExceedingRateLimit_Returns429()
    {
        var tokens = await RegisterUserAsync("msgratelimit", "msgratelimit@example.com", "Rate Limit User", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Rate Limit Server");
        var channel = await CreateChannelAsync(tokens.AccessToken, server!.Id, "rate-limit-channel");

        var url = $"/api/channels/{channel!.Id}/messages";

        // The rate limit is 10 requests per 10 seconds with a queue of 2.
        // Send all requests concurrently so they hit within the same window.
        // Requests beyond 10 + 2 queued = 12 should be rejected with 429.
        var tasks = Enumerable.Range(0, 20).Select(_ =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            return _client.SendAsync(msg);
        }).ToList();

        var responses = await Task.WhenAll(tasks);

        responses.Should().Contain(r => r.StatusCode == HttpStatusCode.TooManyRequests);
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
        var request = new { Name = name, Description = "Test server" };

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/servers");
        msg.Content = JsonContent.Create(request);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(msg);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServerResponse>();
    }

    private async Task<ChannelResponse?> CreateChannelAsync(string accessToken, Guid serverId, string name)
    {
        var request = new { Name = name, Type = "Text" };

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

    private sealed record MessageResponse(
        Guid Id,
        Guid AuthorId,
        Guid ChannelId,
        string Content,
        bool IsEdited,
        DateTime CreatedAt);
}
