using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Vox.Api.Tests.Fixtures;

namespace Vox.Api.Tests.Integration;

public sealed class AccountLinkEndpointsIntegrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AccountLinkEndpointsIntegrationTests(AuthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -------------------------------------------------------------------------
    // GET /api/auth/account-links
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetLinkedProviders_WithoutAuthentication_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/account-links");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLinkedProviders_WithAuthentication_Returns200WithEmptyList()
    {
        var tokens = await RegisterAndLoginAsync(
            "linklistuser", "linklistuser@example.com", "Link List User", "Password1");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/account-links");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<LinkedAccountResponse>>();
        body.Should().NotBeNull();
        // A user registered with email/password has no external providers by default.
        body.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // POST /api/auth/account-links
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LinkProvider_WithoutAuthentication_Returns401()
    {
        var request = new { Provider = "Google", ProviderKey = "google-123" };

        var response = await _client.PostAsJsonAsync("/api/auth/account-links", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LinkProvider_WithValidData_Returns204()
    {
        var tokens = await RegisterAndLoginAsync(
            "linkadduser", "linkadduser@example.com", "Link Add User", "Password1");

        var linkRequest = new { Provider = "Google", ProviderKey = "google-unique-key-1" };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/account-links");
        request.Content = JsonContent.Create(linkRequest);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LinkProvider_WhenAlreadyLinked_Returns400()
    {
        var tokens = await RegisterAndLoginAsync(
            "linkdupuser", "linkdupuser@example.com", "Link Dup User", "Password1");

        var linkRequest = new { Provider = "Google", ProviderKey = "google-dup-key" };

        // Link once.
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/account-links");
        firstRequest.Content = JsonContent.Create(linkRequest);
        firstRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var firstResponse = await _client.SendAsync(firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Linking the same provider again should fail.
        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/account-links");
        secondRequest.Content = JsonContent.Create(linkRequest);
        secondRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var secondResponse = await _client.SendAsync(secondRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/auth/account-links/{provider}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UnlinkProvider_WithoutAuthentication_Returns401()
    {
        var response = await _client.DeleteAsync("/api/auth/account-links/Google");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnlinkProvider_WhenProviderLinked_Returns204()
    {
        var tokens = await RegisterAndLoginAsync(
            "unlinkuser", "unlinkuser@example.com", "Unlink User", "Password1");

        // Link a provider first.
        var linkRequest = new { Provider = "GitHub", ProviderKey = "github-unlink-key" };
        using var linkMsg = new HttpRequestMessage(HttpMethod.Post, "/api/auth/account-links");
        linkMsg.Content = JsonContent.Create(linkRequest);
        linkMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var linkResponse = await _client.SendAsync(linkMsg);
        linkResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Now unlink it.
        using var unlinkMsg = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/account-links/GitHub");
        unlinkMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var unlinkResponse = await _client.SendAsync(unlinkMsg);

        unlinkResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UnlinkProvider_WhenOnlyPasswordExists_WithNoProviders_Returns400()
    {
        // A user registered with password only has no external providers.
        // Attempting to unlink a provider that was never linked should fail.
        var tokens = await RegisterAndLoginAsync(
            "noprovideruser", "noprovideruser@example.com", "No Provider User", "Password1");

        using var unlinkMsg = new HttpRequestMessage(
            HttpMethod.Delete, "/api/auth/account-links/Google");
        unlinkMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(unlinkMsg);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetLinkedProviders_AfterLinking_ReturnsLinkedProvider()
    {
        var tokens = await RegisterAndLoginAsync(
            "linkverifyuser", "linkverifyuser@example.com", "Link Verify User", "Password1");

        // Link a provider.
        var linkRequest = new { Provider = "Microsoft", ProviderKey = "ms-verify-key" };
        using var linkMsg = new HttpRequestMessage(HttpMethod.Post, "/api/auth/account-links");
        linkMsg.Content = JsonContent.Create(linkRequest);
        linkMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var linkResponse = await _client.SendAsync(linkMsg);
        linkResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the provider appears in the list.
        using var getMsg = new HttpRequestMessage(HttpMethod.Get, "/api/auth/account-links");
        getMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var getResponse = await _client.SendAsync(getMsg);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var providers = await getResponse.Content.ReadFromJsonAsync<List<LinkedAccountResponse>>();
        providers.Should().NotBeNull();
        providers!.Should().ContainSingle(p => p.Provider == "Microsoft");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<AuthTokensResponse> RegisterAndLoginAsync(
        string userName, string email, string displayName, string password)
    {
        var registerRequest = new { UserName = userName, Email = email, DisplayName = displayName, Password = password };
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        return tokens!;
    }

    private sealed record AuthTokensResponse(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAt,
        string UserId,
        string Email,
        string UserName,
        string DisplayName);

    private sealed record LinkedAccountResponse(
        string Provider,
        string ProviderKey,
        string? ProviderDisplayName);
}
