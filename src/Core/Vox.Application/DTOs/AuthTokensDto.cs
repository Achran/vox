namespace Vox.Application.DTOs;

public sealed record AuthTokensDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    string UserId,
    string Email,
    string UserName,
    string DisplayName
);
