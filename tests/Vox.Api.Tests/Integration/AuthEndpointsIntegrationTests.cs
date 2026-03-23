using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Vox.Api.Tests.Fixtures;

namespace Vox.Api.Tests.Integration;

public sealed class AuthEndpointsIntegrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsIntegrationTests(AuthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -------------------------------------------------------------------------
    // POST /api/auth/register
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Register_WithValidData_Returns200WithTokens()
    {
        var request = new
        {
            UserName = "integuser1",
            Email = "integuser1@example.com",
            DisplayName = "Integ User 1",
            Password = "Password1"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.UserId.Should().NotBeNullOrEmpty();
        body.Email.Should().Be(request.Email);
        body.UserName.Should().Be(request.UserName);
        body.DisplayName.Should().Be(request.DisplayName);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        var request = new
        {
            UserName = "dupeuser",
            Email = "dupe@example.com",
            DisplayName = "Dupe User",
            Password = "Password1"
        };

        // First registration succeeds.
        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second registration with the same email must fail.
        var secondRequest = new
        {
            UserName = "dupeuser2",
            Email = "dupe@example.com",
            DisplayName = "Dupe User 2",
            Password = "Password1"
        };
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/register", secondRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("ab", "valid@example.com", "Name", "Password1")]   // username too short
    [InlineData("validuser", "not-an-email", "Name", "Password1")] // invalid email
    [InlineData("validuser", "valid@example.com", "", "Password1")] // empty display name
    [InlineData("validuser", "valid@example.com", "Name", "short")] // weak password
    public async Task Register_WithInvalidInput_Returns400(
        string userName, string email, string displayName, string password)
    {
        var request = new { UserName = userName, Email = email, DisplayName = displayName, Password = password };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // POST /api/auth/login
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        // Register first.
        var email = "logintest@example.com";
        var password = "Password1";
        await RegisterUserAsync("loginuser", email, "Login User", password);

        var loginRequest = new { Email = email, Password = password };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await RegisterUserAsync("badpassuser", "badpass@example.com", "Bad Pass User", "Password1");

        var loginRequest = new { Email = "badpass@example.com", Password = "WrongPassword1" };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var loginRequest = new { Email = "nobody@example.com", Password = "Password1" };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", "Password1")]         // empty email
    [InlineData("not-an-email", "pass")]  // invalid email format
    [InlineData("valid@example.com", "")] // empty password
    public async Task Login_WithInvalidInput_Returns400(string email, string password)
    {
        var request = new { Email = email, Password = password };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // POST /api/auth/refresh
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Refresh_WithValidToken_Returns200WithNewTokens()
    {
        var tokens = await RegisterUserAsync("refreshuser", "refresh@example.com", "Refresh User", "Password1");

        var refreshRequest = new { RefreshToken = tokens!.RefreshToken };

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        var refreshRequest = new { RefreshToken = "completely-invalid-refresh-token" };

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // POST /api/auth/revoke
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Revoke_WithValidTokenAndAuthenticated_Returns204()
    {
        var tokens = await RegisterUserAsync("revokeuser", "revoke@example.com", "Revoke User", "Password1");

        var revokeRequest = new { RefreshToken = tokens!.RefreshToken };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/auth/revoke");
        requestMessage.Content = JsonContent.Create(revokeRequest);
        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.SendAsync(requestMessage);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Revoke_WithoutAuthentication_Returns401()
    {
        var revokeRequest = new { RefreshToken = "some-token" };

        var response = await _client.PostAsJsonAsync("/api/auth/revoke", revokeRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // GET /api/auth/me
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Me_WithValidJwt_Returns200WithUserClaims()
    {
        var tokens = await RegisterUserAsync("meuser", "meuser@example.com", "Me User", "Password1");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MeResponse>();
        body.Should().NotBeNull();
        body!.Email.Should().Be("meuser@example.com");
        body.UserName.Should().Be("meuser");
        body.DisplayName.Should().Be("Me User");
        body.UserId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Me_WithoutJwt_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // JWT validation scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Me_WithExpiredJwt_Returns401()
    {
        var expiredToken = TestJwtHelper.GenerateExpiredToken();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithTamperedJwt_Returns401()
    {
        var validToken = TestJwtHelper.GenerateToken("user-id", "t@example.com", "user", "User");
        // Tamper with the signature by appending extra characters.
        var tamperedToken = validToken + "tampered";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithTokenSignedByWrongKey_Returns401()
    {
        // Generate a token with a different secret.
        var wrongKeyToken = TestJwtHelper.GenerateTokenWithCustomSecret(
            "user-id", "t@example.com", "user", "User",
            "a-completely-different-secret-key-32chars!");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", wrongKeyToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<AuthTokensResponse?> RegisterUserAsync(
        string userName, string email, string displayName, string password)
    {
        var request = new { UserName = userName, Email = email, DisplayName = displayName, Password = password };
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
    }

    private sealed record AuthTokensResponse(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAt,
        string UserId,
        string Email,
        string UserName,
        string DisplayName);

    private sealed record MeResponse(
        string? UserId,
        string? Email,
        string? UserName,
        string? DisplayName);
}
