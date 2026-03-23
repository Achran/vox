using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Vox.Shared.UI.Auth;

/// <summary>
/// Blazor AuthenticationStateProvider backed by a stored JWT access token.
/// The token is loaded from <see cref="ITokenStorageService"/> on the first access
/// and re-evaluated whenever the user logs in or out.
/// </summary>
public sealed class VoxAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly ITokenStorageService _tokenStorage;
    private AuthenticationState? _cachedState;

    public VoxAuthenticationStateProvider(ITokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_cachedState is not null)
            return _cachedState;

        var token = await _tokenStorage.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var claims = ParseClaimsFromJwt(token);
        if (claims is null || IsTokenExpired(claims))
        {
            await _tokenStorage.ClearTokensAsync();
            return Anonymous;
        }

        _cachedState = BuildState(claims);
        return _cachedState;
    }

    /// <summary>Called after successful login / external auth callback.</summary>
    public void NotifyUserAuthenticated(string accessToken)
    {
        var claims = ParseClaimsFromJwt(accessToken);
        _cachedState = claims is not null ? BuildState(claims) : Anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
    }

    /// <summary>Called after logout.</summary>
    public void NotifyUserLoggedOut()
    {
        _cachedState = Anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static AuthenticationState BuildState(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private static bool IsTokenExpired(IEnumerable<Claim> claims)
    {
        var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
        if (expClaim is null) return false;

        if (!long.TryParse(expClaim.Value, out var exp)) return true;
        var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);
        return expiry <= DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Decodes the payload of a JWT and returns the claims.
    /// No signature validation is performed — validation happens on the API.
    /// </summary>
    private static IReadOnlyList<Claim>? ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return null;

        try
        {
            var payload = Base64UrlDecode(parts[1]);
            using var doc = JsonDocument.Parse(payload);

            var claims = new List<Claim>();
            foreach (var element in doc.RootElement.EnumerateObject())
            {
                if (element.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.Value.EnumerateArray())
                        claims.Add(new Claim(element.Name, GetClaimValue(item)));
                }
                else
                {
                    claims.Add(new Claim(element.Name, GetClaimValue(element.Value)));
                }
            }

            return claims;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        output = (output.Length % 4) switch
        {
            2 => output + "==",
            3 => output + "=",
            _ => output
        };
        return Convert.FromBase64String(output);
    }

    /// <summary>
    /// Extracts the string representation of a JSON element suitable for use as a claim value.
    /// Strings are returned unquoted; numbers are returned as their raw text;
    /// booleans are returned as lowercase "true"/"false".
    /// </summary>
    private static string GetClaimValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? string.Empty,
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True   => "true",
        JsonValueKind.False  => "false",
        _                    => element.GetRawText()
    };
}
