using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Vox.Api.Tests.Fixtures;

namespace Vox.Api.Tests.Integration;

public sealed class ServerEndpointsIntegrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ServerEndpointsIntegrationTests(AuthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -------------------------------------------------------------------------
    // POST /api/servers
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateServer_WithValidData_Returns201()
    {
        var tokens = await RegisterUserAsync("srvowner1", "srvowner1@example.com", "Server Owner 1", "Password1");

        var request = new { Name = "My Server", Description = "A test server" };

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/servers");
        msg.Content = JsonContent.Create(request);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ServerResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("My Server");
        body.Description.Should().Be("A test server");
        body.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateServer_WithoutAuthentication_Returns401()
    {
        var request = new { Name = "My Server", Description = "A test server" };

        var response = await _client.PostAsJsonAsync("/api/servers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateServer_WithInvalidData_Returns400()
    {
        var tokens = await RegisterUserAsync("srvinvalid1", "srvinvalid1@example.com", "Invalid Server", "Password1");

        var request = new { Name = "", Description = "A test server" };

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/servers");
        msg.Content = JsonContent.Create(request);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // GET /api/servers/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetServerById_WithExistingServer_Returns200()
    {
        var tokens = await RegisterUserAsync("srvget1", "srvget1@example.com", "Server Get", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Get Server", "Test");

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/servers/{server!.Id}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ServerResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Get Server");
    }

    [Fact]
    public async Task GetServerById_WithNonExistentId_Returns404()
    {
        var tokens = await RegisterUserAsync("srvget404", "srvget404@example.com", "Server Get 404", "Password1");

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/api/servers/{Guid.NewGuid()}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // GET /api/servers
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserServers_WithAuthentication_Returns200()
    {
        var tokens = await RegisterUserAsync("srvlist1", "srvlist1@example.com", "Server List", "Password1");
        await CreateServerAsync(tokens.AccessToken, "Listed Server", null);

        using var msg = new HttpRequestMessage(HttpMethod.Get, "/api/servers");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ServerResponse>>();
        body.Should().NotBeNull();
        body!.Should().Contain(s => s.Name == "Listed Server");
    }

    [Fact]
    public async Task GetUserServers_WithoutAuthentication_Returns401()
    {
        var response = await _client.GetAsync("/api/servers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // PUT /api/servers/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateServer_AsOwner_Returns200()
    {
        var tokens = await RegisterUserAsync("srvupd1", "srvupd1@example.com", "Server Update", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Original Name", "Original Desc");

        var updateRequest = new { Name = "Updated Name", Description = "Updated Desc" };

        using var msg = new HttpRequestMessage(HttpMethod.Put, $"/api/servers/{server!.Id}");
        msg.Content = JsonContent.Create(updateRequest);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ServerResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Updated Name");
        body.Description.Should().Be("Updated Desc");
    }

    [Fact]
    public async Task UpdateServer_AsNonOwner_Returns403()
    {
        var ownerTokens = await RegisterUserAsync("srvupdowner", "srvupdowner@example.com", "Owner", "Password1");
        var otherTokens = await RegisterUserAsync("srvupdother", "srvupdother@example.com", "Other", "Password1");
        var server = await CreateServerAsync(ownerTokens.AccessToken, "Owner Server", null);

        var updateRequest = new { Name = "Hacked Name" };

        using var msg = new HttpRequestMessage(HttpMethod.Put, $"/api/servers/{server!.Id}");
        msg.Content = JsonContent.Create(updateRequest);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherTokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateServer_WithNonExistentId_Returns404()
    {
        var tokens = await RegisterUserAsync("srvupd404", "srvupd404@example.com", "Update 404", "Password1");

        var updateRequest = new { Name = "Updated Name" };

        using var msg = new HttpRequestMessage(HttpMethod.Put, $"/api/servers/{Guid.NewGuid()}");
        msg.Content = JsonContent.Create(updateRequest);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/servers/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteServer_AsOwner_Returns204()
    {
        var tokens = await RegisterUserAsync("srvdel1", "srvdel1@example.com", "Server Delete", "Password1");
        var server = await CreateServerAsync(tokens.AccessToken, "Delete Me", null);

        using var msg = new HttpRequestMessage(HttpMethod.Delete, $"/api/servers/{server!.Id}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteServer_AsNonOwner_Returns403()
    {
        var ownerTokens = await RegisterUserAsync("srvdelowner", "srvdelowner@example.com", "Owner", "Password1");
        var otherTokens = await RegisterUserAsync("srvdelother", "srvdelother@example.com", "Other", "Password1");
        var server = await CreateServerAsync(ownerTokens.AccessToken, "Owner Only Server", null);

        using var msg = new HttpRequestMessage(HttpMethod.Delete, $"/api/servers/{server!.Id}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherTokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteServer_WithNonExistentId_Returns404()
    {
        var tokens = await RegisterUserAsync("srvdel404", "srvdel404@example.com", "Delete 404", "Password1");

        using var msg = new HttpRequestMessage(HttpMethod.Delete, $"/api/servers/{Guid.NewGuid()}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /api/servers/{id}/join
    // -------------------------------------------------------------------------

    [Fact]
    public async Task JoinServer_WithValidServer_Returns200()
    {
        var ownerTokens = await RegisterUserAsync("srvjoinowner", "srvjoinowner@example.com", "Owner", "Password1");
        var joinerTokens = await RegisterUserAsync("srvjoiner", "srvjoiner@example.com", "Joiner", "Password1");
        var server = await CreateServerAsync(ownerTokens.AccessToken, "Joinable Server", null);

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/servers/{server!.Id}/join");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", joinerTokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JoinServer_WithNonExistentServer_Returns404()
    {
        var tokens = await RegisterUserAsync("srvjoin404", "srvjoin404@example.com", "Join 404", "Password1");

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/servers/{Guid.NewGuid()}/join");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(msg);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task JoinServer_WithoutAuthentication_Returns401()
    {
        var response = await _client.PostAsync($"/api/servers/{Guid.NewGuid()}/join", null);

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

    private async Task<ServerResponse?> CreateServerAsync(string accessToken, string name, string? description)
    {
        var request = new { Name = name, Description = description };

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/servers");
        msg.Content = JsonContent.Create(request);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(msg);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServerResponse>();
    }

    private sealed record ServerResponse(
        Guid Id,
        string Name,
        string? Description,
        string? IconUrl,
        Guid OwnerId,
        DateTime CreatedAt);
}
