using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Vox.Shared.UI.Services;

namespace Vox.Shared.UI.Auth;

public static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Vox auth services (token storage, auth service, state provider)
    /// and application services for use in both Blazor WebAssembly and MAUI Hybrid clients.
    /// </summary>
    public static IServiceCollection AddVoxAuth(this IServiceCollection services)
    {
        services.AddAuthorizationCore();
        services.AddScoped<ITokenStorageService, LocalStorageTokenStorageService>();
        services.AddScoped<VoxAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<VoxAuthenticationStateProvider>());
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IServerService, ServerService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}
