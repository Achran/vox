namespace Vox.Shared.UI.Services;

public interface IVoiceService : IAsyncDisposable
{
    bool IsConnected { get; }
    string? CurrentChannelId { get; }
    IReadOnlyList<string> Participants { get; }
    bool IsMuted { get; }

    event Action<string, string>? UserJoined;
    event Action<string, string>? UserLeft;
    event Action<string, IReadOnlyList<string>>? ParticipantsUpdated;
    event Action? StateChanged;

    Task ConnectAsync();
    Task JoinChannelAsync(string channelId);
    Task LeaveChannelAsync(string channelId);
    void ToggleMute();
}
