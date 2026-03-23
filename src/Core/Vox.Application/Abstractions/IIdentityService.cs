using Vox.Application.DTOs;

namespace Vox.Application.Abstractions;

public interface IIdentityService
{
    /// <summary>
    /// Creates a new user account and returns tokens for immediate login.
    /// Throws <see cref="InvalidOperationException"/> if registration fails (e.g. duplicate email).
    /// </summary>
    Task<AuthTokensDto> RegisterAsync(
        string userName,
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates credentials and returns tokens.
    /// Throws <see cref="UnauthorizedAccessException"/> if credentials are invalid.
    /// </summary>
    Task<AuthTokensDto> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a valid refresh token for a new token pair (rotation).
    /// Throws <see cref="UnauthorizedAccessException"/> if the token is invalid or expired.
    /// </summary>
    Task<AuthTokensDto> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token (logout). Silent if the token is not found.
    /// </summary>
    Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds or creates a user from an external login provider and returns tokens.
    /// If a user with the same email already exists, links the external login to the existing user.
    /// Throws <see cref="InvalidOperationException"/> if user creation fails.
    /// </summary>
    Task<AuthTokensDto> ExternalLoginAsync(
        string provider,
        string providerKey,
        string? email,
        string? displayName,
        CancellationToken cancellationToken = default);
}
