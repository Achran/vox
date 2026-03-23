namespace Vox.Shared.UI.Auth;

public interface IAuthService
{
    /// <summary>Returns tokens on success, null on failure (sets ErrorMessage).</summary>
    Task<AuthTokensResponse?> LoginAsync(string email, string password);

    /// <summary>Returns tokens on success, null on failure (sets ErrorMessage).</summary>
    Task<AuthTokensResponse?> RegisterAsync(
        string userName, string email, string displayName, string password);

    /// <summary>Revokes the stored refresh token and clears local state.</summary>
    Task LogoutAsync();

    /// <summary>Returns linked external accounts for the currently authenticated user.</summary>
    Task<IReadOnlyList<LinkedAccountResponse>> GetLinkedProvidersAsync();

    /// <summary>Removes the specified external provider link for the current user.</summary>
    Task<bool> UnlinkProviderAsync(string provider);

    /// <summary>Returns the URL that initiates the external OAuth challenge on the API.</summary>
    string GetExternalLoginUrl(string provider);

    /// <summary>The last error message from a failed operation, or null.</summary>
    string? ErrorMessage { get; }
}
