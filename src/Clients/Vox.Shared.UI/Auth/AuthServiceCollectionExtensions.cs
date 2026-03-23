using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Vox.Shared.UI.Auth;

public static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Vox auth services (token storage, auth service, state provider)
    /// for use in both Blazor WebAssembly and MAUI Hybrid clients.
    /// </summary>
    public static IServiceCollection AddVoxAuth(this IServiceCollection services)
    {
        services.AddAuthorizationCore();
        services.AddScoped<ITokenStorageService, LocalStorageTokenStorageService>();
        services.AddScoped<VoxAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<VoxAuthenticationStateProvider>());
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
