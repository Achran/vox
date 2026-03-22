namespace Vox.Infrastructure.Identity;

public sealed class ExternalAuthSettings
{
    public string ClientCallbackUrl { get; init; } = "/auth/external-callback";
    public OAuthProviderSettings? Google { get; init; }
    public OAuthProviderSettings? Microsoft { get; init; }
    public OAuthProviderSettings? Facebook { get; init; }
    public OAuthProviderSettings? Twitter { get; init; }
    public OAuthProviderSettings? GitHub { get; init; }
    public SteamProviderSettings? Steam { get; init; }
}

public sealed class OAuthProviderSettings
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}

public sealed class SteamProviderSettings
{
    public string ApplicationKey { get; init; } = string.Empty;
}
