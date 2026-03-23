namespace Vox.Api.Tests.Fixtures;

/// <summary>
/// Shared response record used across integration test classes for deserializing
/// authentication token responses from the API.
/// </summary>
public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    string UserId,
    string Email,
    string UserName,
    string DisplayName);
