namespace Vox.Shared.UI.Services;

public interface IVoiceService : IAsyncDisposable
{
    bool IsConnected { get; }
    string? CurrentChannelId { get; }
    IReadOnlyList<string> Participants { get; }
    bool IsMuted { get; }

    /// <summary>User IDs of participants who are currently speaking (from LiveKit active-speaker events).</summary>
    IReadOnlySet<string> ActiveSpeakers { get; }

    /// <summary>Mute states of remote participants (userId → isMuted), updated via SignalR broadcast.</summary>
    IReadOnlyDictionary<string, bool> ParticipantMuteStates { get; }

    event Action<string, string>? UserJoined;
    event Action<string, string>? UserLeft;
    event Action<string, IReadOnlyList<string>>? ParticipantsUpdated;
    event Action? StateChanged;

    Task ConnectAsync();
    Task JoinChannelAsync(string channelId);
    Task LeaveChannelAsync(string channelId);
    void ToggleMute();
}
