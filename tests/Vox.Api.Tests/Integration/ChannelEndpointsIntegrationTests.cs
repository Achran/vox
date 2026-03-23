using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Vox.Api.Tests.Fixtures;

namespace Vox.Api.Tests.Integration;

public sealed class ChannelEndpointsIntegrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ChannelEndpointsIntegrationTests(AuthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -------------------------------------------------------------------------
    // POST /api/servers/{serverId}/channels
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateChannel_AsOwner_Returns201()
    {
        var tokens = await RegisterUserAsync("chowner1", "chowner1@example.com", "Channel Owner 1", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Channel Server 1");

        var request = new { Name = "new-channel", Type = "Text" };

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/servers/{server!.Id}/channels");
        msg.Content = JsonContent.Create(request);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ChannelResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("new-channel");
        body.Type.Should().Be("Text");
        body.ServerId.Should().Be(server.Id);
    }

    [Fact]
    public async Task CreateChannel_AsNonOwner_Returns403()
    {
        var ownerTokens = await RegisterUserAsync("chcreateowner", "chcreateowner@example.com", "Owner", "Password1");
        var otherTokens = await RegisterUserAsync("chcreateother", "chcreateother@example.com", "Other", "Password1");
        var server = await CreateServerAsync(ownerTokens.AccessToken, "Owner Channel Server");

        var request = new { Name = "hacked-channel", Type = "Text" };

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/servers/{server!.Id}/channels");
        msg.Content = JsonContent.Create(request);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherTokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateChannel_WithoutAuthentication_Returns401()
    {
        var request = new { Name = "new-channel", Type = "Text" };

        var response = await _client.PostAsJsonAsync($"/api/servers/{Guid.NewGuid()}/channels", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateChannel_WithInvalidType_Returns400()
    {
        var tokens = await RegisterUserAsync("chinvtype", "chinvtype@example.com", "Invalid Type", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Invalid Type Server");

        var request = new { Name = "test-channel", Type = "InvalidType" };

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/servers/{server!.Id}/channels");
        msg.Content = JsonContent.Create(request);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // GET /api/servers/{serverId}/channels
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetServerChannels_WithExistingServer_Returns200()
    {
        var tokens = await RegisterUserAsync("chlist1", "chlist1@example.com", "Channel List", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Channels List Server");

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/servers/{server!.Id}/channels");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ChannelResponse>>();
        body.Should().NotBeNull();
        // Server.Create adds default "general" text and "General" voice channels
        body!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    // -------------------------------------------------------------------------
    // GET /api/channels/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetChannelById_WithExistingChannel_Returns200()
    {
        var tokens = await RegisterUserAsync("chget1", "chget1@example.com", "Channel Get", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Get Channel Server");
        var channel = await CreateChannelAsync(tokens.AccessToken, server!.Id, "get-channel", "Text");

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/channels/{channel!.Id}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ChannelResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("get-channel");
    }

    [Fact]
    public async Task GetChannelById_WithNonExistentId_Returns404()
    {
        var tokens = await RegisterUserAsync("chget404", "chget404@example.com", "Channel Get 404", "Password1");

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/channels/{Guid.NewGuid()}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // PUT /api/channels/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateChannel_AsOwner_Returns200()
    {
        var tokens = await RegisterUserAsync("chupd1", "chupd1@example.com", "Channel Update", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Update Channel Server");
        var channel = await CreateChannelAsync(tokens.AccessToken, server!.Id, "old-name", "Text");

        var updateRequest = new { Name = "new-name" };

        using var msg = new HttpRequestMessage(HttpMethod.Put, $"/api/channels/{channel!.Id}");
        msg.Content = JsonContent.Create(updateRequest);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ChannelResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("new-name");
    }

    [Fact]
    public async Task UpdateChannel_AsNonOwner_Returns403()
    {
        var ownerTokens = await RegisterUserAsync("chupdowner", "chupdowner@example.com", "Owner", "Password1");
        var otherTokens = await RegisterUserAsync("chupdother", "chupdother@example.com", "Other", "Password1");
        var server = await CreateServerAsync(ownerTokens.AccessToken, "Owner Update Server");
        var channel = await CreateChannelAsync(ownerTokens.AccessToken, server!.Id, "protected-channel", "Text");

        var updateRequest = new { Name = "hacked-name" };

        using var msg = new HttpRequestMessage(HttpMethod.Put, $"/api/channels/{channel!.Id}");
        msg.Content = JsonContent.Create(updateRequest);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherTokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateChannel_WithNonExistentId_Returns404()
    {
        var tokens = await RegisterUserAsync("chupd404", "chupd404@example.com", "Update 404", "Password1");

        var updateRequest = new { Name = "new-name" };

        using var msg = new HttpRequestMessage(HttpMethod.Put, $"/api/channels/{Guid.NewGuid()}");
        msg.Content = JsonContent.Create(updateRequest);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/channels/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteChannel_AsOwner_Returns204()
    {
        var tokens = await RegisterUserAsync("chdel1", "chdel1@example.com", "Channel Delete", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Delete Channel Server");
        var channel = await CreateChannelAsync(tokens.AccessToken, server!.Id, "delete-me", "Text");

        using var msg = new HttpRequestMessage(HttpMethod.Delete, $"/api/channels/{channel!.Id}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteChannel_AsNonOwner_Returns403()
    {
        var ownerTokens = await RegisterUserAsync("chdelowner", "chdelowner@example.com", "Owner", "Password1");
        var otherTokens = await RegisterUserAsync("chdelother", "chdelother@example.com", "Other", "Password1");
        var server = await CreateServerAsync(ownerTokens.AccessToken, "Owner Delete Server");
        var channel = await CreateChannelAsync(ownerTokens.AccessToken, server!.Id, "protected-delete", "Text");

        using var msg = new HttpRequestMessage(HttpMethod.Delete, $"/api/channels/{channel!.Id}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherTokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteChannel_WithNonExistentId_Returns404()
    {
        var tokens = await RegisterUserAsync("chdel404", "chdel404@example.com", "Delete 404", "Password1");

        using var msg = new HttpRequestMessage(HttpMethod.Delete, $"/api/channels/{Guid.NewGuid()}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

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
}
