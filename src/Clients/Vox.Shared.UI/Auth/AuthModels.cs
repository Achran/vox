namespace Vox.Shared.UI.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(
    string UserName,
    string Email,
    string DisplayName,
    string Password);

public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    string UserId,
    string Email,
    string UserName,
    string DisplayName);

public sealed record LinkedAccountResponse(
    string Provider,
    string ProviderKey,
    string? ProviderDisplayName);
