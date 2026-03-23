using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
            new("video", JsonSerializer.Serialize(videoGrant)),
            new("jti", Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.ApiKey,
            audience: null,
            claims: claims,
            notBefore: now,
            expires: now.AddHours(6),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
