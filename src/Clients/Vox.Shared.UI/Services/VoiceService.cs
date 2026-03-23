using Microsoft.AspNetCore.SignalR.Client;
using Vox.Shared.UI.Auth;

namespace Vox.Shared.UI.Services;

public sealed class VoiceService : IVoiceService
{
    private readonly ITokenStorageService _tokenStorage;
    private readonly Uri _hubUrl;
    private HubConnection? _hubConnection;
    private readonly object _lock = new();
    private List<string> _participants = [];

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public string? CurrentChannelId { get; private set; }
    public IReadOnlyList<string> Participants
    {
        get { lock (_lock) { return _participants.ToList(); } }
    }
    public bool IsMuted { get; private set; }

    public event Action<string, string>? UserJoined;
    public event Action<string, string>? UserLeft;
    public event Action<string, IReadOnlyList<string>>? ParticipantsUpdated;
    public event Action? StateChanged;

    public VoiceService(HttpClient http, ITokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage;

        if (http.BaseAddress is null)
        {
            throw new InvalidOperationException(
                "HttpClient.BaseAddress must be configured before creating VoiceService.");
        }

        _hubUrl = new Uri(http.BaseAddress, "hubs/voice");
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection is not null && _hubConnection.State != HubConnectionState.Disconnected)
            return;

        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.AccessTokenProvider = async () => await _tokenStorage.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string, string>("UserJoinedVoice", (userId, channelId) =>
        {
            lock (_lock)
            {
                if (!_participants.Contains(userId))
                    _participants.Add(userId);
            }
            UserJoined?.Invoke(userId, channelId);
            StateChanged?.Invoke();
        });

        _hubConnection.On<string, string>("UserLeftVoice", (userId, channelId) =>
        {
            lock (_lock)
            {
                _participants.Remove(userId);
            }
            UserLeft?.Invoke(userId, channelId);
            StateChanged?.Invoke();
        });

        _hubConnection.On<string, IReadOnlyList<string>>("VoiceParticipants", (channelId, participants) =>
        {
            lock (_lock)
            {
                _participants = participants.ToList();
            }
            ParticipantsUpdated?.Invoke(channelId, participants);
            StateChanged?.Invoke();
        });

        _hubConnection.Reconnected += async _ =>
        {
            if (CurrentChannelId is not null)
            {
                await _hubConnection.InvokeAsync("JoinVoiceChannel", CurrentChannelId);
            }
        };

        await _hubConnection.StartAsync();
    }

    public async Task JoinChannelAsync(string channelId)
    {
        if (CurrentChannelId == channelId)
            return;

        if (CurrentChannelId is not null)
        {
            await LeaveChannelAsync(CurrentChannelId);
        }

        CurrentChannelId = channelId;

        if (IsConnected)
        {
            await _hubConnection!.InvokeAsync("JoinVoiceChannel", channelId);
        }

        StateChanged?.Invoke();
    }

    public async Task LeaveChannelAsync(string channelId)
    {
        if (CurrentChannelId != channelId)
            return;

        if (IsConnected)
        {
            await _hubConnection!.InvokeAsync("LeaveVoiceChannel", channelId);
        }

        CurrentChannelId = null;
        lock (_lock)
        {
            _participants = [];
        }
        IsMuted = false;
        StateChanged?.Invoke();
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        StateChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            if (CurrentChannelId is not null && IsConnected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("LeaveVoiceChannel", CurrentChannelId);
                }
                catch
                {
                    // Ignore errors during disposal
                }
            }

            await _hubConnection.DisposeAsync();
        }
    }
}
