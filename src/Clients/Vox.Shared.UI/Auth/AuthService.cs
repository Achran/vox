using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vox.Shared.UI.Auth;

public sealed class AuthService : IAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly ITokenStorageService _tokenStorage;
    private readonly VoxAuthenticationStateProvider _authStateProvider;

    public string? ErrorMessage { get; private set; }

    public AuthService(
        HttpClient http,
        ITokenStorageService tokenStorage,
        VoxAuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _tokenStorage = tokenStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<AuthTokensResponse?> LoginAsync(string email, string password)
    {
        ErrorMessage = null;
        try
        {
            var response = await _http.PostAsJsonAsync(
                "api/auth/login",
                new LoginRequest(email, password),
                JsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ReadErrorAsync(response);
                return null;
            }

            var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(JsonOptions);
            if (tokens is null) return null;

            await _tokenStorage.SetTokensAsync(tokens.AccessToken, tokens.RefreshToken);
            _authStateProvider.NotifyUserAuthenticated(tokens.AccessToken);
            return tokens;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
    }

    public async Task<AuthTokensResponse?> RegisterAsync(
        string userName, string email, string displayName, string password)
    {
        ErrorMessage = null;
        try
        {
            var response = await _http.PostAsJsonAsync(
                "api/auth/register",
                new RegisterRequest(userName, email, displayName, password),
                JsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ReadErrorAsync(response);
                return null;
            }

            var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(JsonOptions);
            if (tokens is null) return null;

            await _tokenStorage.SetTokensAsync(tokens.AccessToken, tokens.RefreshToken);
            _authStateProvider.NotifyUserAuthenticated(tokens.AccessToken);
            return tokens;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        ErrorMessage = null;
        try
        {
            var refreshToken = await _tokenStorage.GetRefreshTokenAsync();
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var accessToken = await _tokenStorage.GetAccessTokenAsync();
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/revoke");
                request.Content = JsonContent.Create(new { RefreshToken = refreshToken }, options: JsonOptions);
                if (!string.IsNullOrEmpty(accessToken))
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                await _http.SendAsync(request);
            }
        }
        catch
        {
            // Best effort — always clear local state
        }
        finally
        {
            await _tokenStorage.ClearTokensAsync();
            _authStateProvider.NotifyUserLoggedOut();
        }
    }

    public async Task<IReadOnlyList<LinkedAccountResponse>> GetLinkedProvidersAsync()
    {
        ErrorMessage = null;
        try
        {
            var accessToken = await _tokenStorage.GetAccessTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/account-links/");
            if (!string.IsNullOrEmpty(accessToken))
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ReadErrorAsync(response);
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<LinkedAccountResponse>>(JsonOptions)
                   ?? [];        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return [];
        }
    }

    public async Task<bool> UnlinkProviderAsync(string provider)
    {
        ErrorMessage = null;
        try
        {
            var accessToken = await _tokenStorage.GetAccessTokenAsync();
            using var request = new HttpRequestMessage(
                HttpMethod.Delete, $"api/auth/account-links/{Uri.EscapeDataString(provider)}");
            if (!string.IsNullOrEmpty(accessToken))
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ReadErrorAsync(response);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    public string GetExternalLoginUrl(string provider)
    {
        var baseAddress = _http.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
        return $"{baseAddress}/api/auth/external/{Uri.EscapeDataString(provider)}";
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());
            if (doc.RootElement.TryGetProperty("error", out var errProp))
                return errProp.GetString() ?? response.ReasonPhrase ?? "Unknown error";
        }
        catch { }

        return response.ReasonPhrase ?? $"HTTP {(int)response.StatusCode}";
    }
}
