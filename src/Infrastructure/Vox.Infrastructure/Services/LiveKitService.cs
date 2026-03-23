using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Vox.Application.Abstractions;

namespace Vox.Infrastructure.Services;

public sealed class LiveKitService : ILiveKitService
{
    private readonly LiveKitSettings _settings;
    private readonly HttpClient _httpClient;

    public LiveKitService(IOptions<LiveKitSettings> settings, HttpClient httpClient)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
    }

    public string GenerateToken(string userId, string displayName, string roomName)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
            throw new InvalidOperationException("LiveKit:ApiKey is not configured.");
        if (string.IsNullOrEmpty(_settings.ApiSecret))
            throw new InvalidOperationException("LiveKit:ApiSecret is not configured.");

        var now = DateTime.UtcNow;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.ApiSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var videoGrant = new Dictionary<string, object>
        {
            ["roomJoin"] = true,
            ["room"] = roomName
        };

        var claims = new List<Claim>
        {
            new("sub", userId),
            new("name", displayName),
            new("jti", Guid.NewGuid().ToString())
        };

        var payload = new JwtPayload(
            issuer: _settings.ApiKey,
            audience: null,
            claims: claims,
            notBefore: now,
            expires: now.AddHours(6));

        payload["video"] = videoGrant;

        var token = new JwtSecurityToken(
            new JwtHeader(credentials),
            payload);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GetServerUrl() => _settings.Url;

    public async Task<string> CreateRoomAsync(string roomName, CancellationToken cancellationToken = default)
    {
        var token = GenerateServiceToken(roomCreate: true);
        var request = CreateTwirpRequest("livekit.RoomService/CreateRoom", token);
        request.Content = JsonContent.Create(new { name = roomName });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return roomName;
    }

    public async Task DeleteRoomAsync(string roomName, CancellationToken cancellationToken = default)
    {
        var token = GenerateServiceToken(roomAdmin: true);
        var request = CreateTwirpRequest("livekit.RoomService/DeleteRoom", token);
        request.Content = JsonContent.Create(new { room = roomName });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Generates a server-to-server JWT with the specified room management grants.
    /// </summary>
    internal string GenerateServiceToken(bool roomCreate = false, bool roomList = false, bool roomAdmin = false)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
            throw new InvalidOperationException("LiveKit:ApiKey is not configured.");
        if (string.IsNullOrEmpty(_settings.ApiSecret))
            throw new InvalidOperationException("LiveKit:ApiSecret is not configured.");

        var now = DateTime.UtcNow;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.ApiSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var videoGrant = new Dictionary<string, object>();
        if (roomCreate) videoGrant["roomCreate"] = true;
        if (roomList) videoGrant["roomList"] = true;
        if (roomAdmin) videoGrant["roomAdmin"] = true;

        var claims = new List<Claim>
        {
            new("jti", Guid.NewGuid().ToString())
        };

        var payload = new JwtPayload(
            issuer: _settings.ApiKey,
            audience: null,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(10));

        payload["video"] = videoGrant;

        var token = new JwtSecurityToken(
            new JwtHeader(credentials),
            payload);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private HttpRequestMessage CreateTwirpRequest(string method, string token)
    {
        if (string.IsNullOrWhiteSpace(_settings.Url))
            throw new InvalidOperationException("LiveKit:Url is not configured.");

        // LiveKit Twirp API uses the HTTP URL, not the WebSocket URL
        var uri = new Uri(_settings.Url);
        var httpScheme = uri.Scheme switch
        {
            "wss" => "https",
            "ws" => "http",
            _ => uri.Scheme
        };
        var builder = new UriBuilder(uri) { Scheme = httpScheme };
        var httpUrl = builder.Uri.GetLeftPart(UriPartial.Authority);

        var request = new HttpRequestMessage(HttpMethod.Post, $"{httpUrl}/twirp/{method}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
