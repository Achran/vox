namespace Vox.Shared.UI.Auth;

public interface ITokenStorageService
{
    ValueTask<string?> GetAccessTokenAsync();
    ValueTask<string?> GetRefreshTokenAsync();
    ValueTask SetTokensAsync(string accessToken, string refreshToken);
    ValueTask ClearTokensAsync();
}
