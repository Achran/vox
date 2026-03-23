using Microsoft.JSInterop;

namespace Vox.Shared.UI.Auth;

/// <summary>
/// Persists tokens in the browser's localStorage via JS interop.
/// Works for both Blazor WebAssembly and MAUI Hybrid (WebView).
/// </summary>
public sealed class LocalStorageTokenStorageService : ITokenStorageService
{
    private const string AccessTokenKey = "vox_access_token";
    private const string RefreshTokenKey = "vox_refresh_token";

    private readonly IJSRuntime _js;

    public LocalStorageTokenStorageService(IJSRuntime js)
    {
        _js = js;
    }

    public ValueTask<string?> GetAccessTokenAsync()
        => _js.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);

    public ValueTask<string?> GetRefreshTokenAsync()
        => _js.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);

    public async ValueTask SetTokensAsync(string accessToken, string refreshToken)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
        await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
    }

    public async ValueTask ClearTokensAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
    }
}
