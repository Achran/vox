using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Vox.Application.Abstractions;

namespace Vox.Infrastructure.Services;

public sealed class LiveKitService : ILiveKitService
{
    private readonly LiveKitSettings _settings;

    public LiveKitService(IOptions<LiveKitSettings> settings)
    {
        _settings = settings.Value;
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
}
