namespace Vox.Shared.UI.Services;

public interface IPresenceService : IAsyncDisposable
{
    IReadOnlyList<OnlineUserInfo> OnlineUsers { get; }
    event Action? OnUsersChanged;
    Task StartAsync();
    Task StopAsync();
}
