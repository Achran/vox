namespace Vox.Shared.UI.Services;

public interface IChatService : IAsyncDisposable
{
    event Action<MessageResponse>? MessageReceived;
    event Action<string, string>? UserTyping;

    bool IsConnected { get; }

    Task ConnectAsync();
    Task JoinChannelAsync(string channelId);
    Task LeaveChannelAsync(string channelId);
    Task SendMessageAsync(string channelId, string message);
    Task SendTypingAsync(string channelId);
}
