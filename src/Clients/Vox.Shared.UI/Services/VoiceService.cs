using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Vox.Shared.UI.Auth;

namespace Vox.Shared.UI.Services;

public sealed class VoiceService : IVoiceService
{
    private readonly ITokenStorageService _tokenStorage;
    private readonly HttpClient _http;
    private readonly IJSRuntime _jsRuntime;
    private readonly Uri _hubUrl;
    private HubConnection? _hubConnection;
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<VoiceService>? _dotNetRef;
    private readonly object _lock = new();
    private List<string> _participants = [];
    private HashSet<string> _activeSpeakers = [];
    private Dictionary<string, bool> _participantMuteStates = [];

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public string? CurrentChannelId { get; private set; }
    public IReadOnlyList<string> Participants
    {
        get { lock (_lock) { return _participants.ToList(); } }
    }
    public bool IsMuted { get; private set; }

    public IReadOnlySet<string> ActiveSpeakers
    {
        get { lock (_lock) { return new HashSet<string>(_activeSpeakers); } }
    }

    public IReadOnlyDictionary<string, bool> ParticipantMuteStates
    {
        get { lock (_lock) { return new Dictionary<string, bool>(_participantMuteStates); } }
    }

    public event Action<string, string>? UserJoined;
    public event Action<string, string>? UserLeft;
    public event Action<string, IReadOnlyList<string>>? ParticipantsUpdated;
    public event Action? StateChanged;

    public VoiceService(HttpClient http, ITokenStorageService tokenStorage, IJSRuntime jsRuntime)
    {
        _tokenStorage = tokenStorage;
        _http = http;
        _jsRuntime = jsRuntime;

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
                _activeSpeakers.Remove(userId);
                _participantMuteStates.Remove(userId);
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

        _hubConnection.On<string, string, bool>("UserMuteStateChanged", (userId, channelId, isMuted) =>
        {
            lock (_lock)
            {
                _participantMuteStates[userId] = isMuted;
            }
            StateChanged?.Invoke();
        });

        _hubConnection.Reconnected += async _ =>
        {
            if (CurrentChannelId is not null)
            {
                await _hubConnection.InvokeAsync("JoinVoiceChannel", CurrentChannelId);

                // Re-establish LiveKit audio after the SignalR channel rejoin so
                // that actual media keeps working after a network interruption.
                await ConnectLiveKitAsync(CurrentChannelId);
            }
        };

        await _hubConnection.StartAsync();
    }

    public async Task JoinChannelAsync(string channelId)
    {
        // If we're already connected and in the requested channel, nothing to do.
        if (CurrentChannelId == channelId && IsConnected)
            return;

        // If we're in a different channel, leave it first.
        if (CurrentChannelId is not null && CurrentChannelId != channelId)
        {
            await LeaveChannelAsync(CurrentChannelId);
        }

        if (!IsConnected)
        {
            await ConnectAsync();
        }

        try
        {
            await _hubConnection!.InvokeAsync("JoinVoiceChannel", channelId);
            CurrentChannelId = channelId;
        }
        catch
        {
            // Ensure a failed join does not leave a stale CurrentChannelId
            CurrentChannelId = null;
            throw;
        }

        // Connect to the LiveKit room for actual audio
        await ConnectLiveKitAsync(channelId);

        StateChanged?.Invoke();
    }

    public async Task LeaveChannelAsync(string channelId)
    {
        if (CurrentChannelId != channelId)
            return;

        // Disconnect from LiveKit first
        await DisconnectLiveKitAsync();

        if (IsConnected)
        {
            await _hubConnection!.InvokeAsync("LeaveVoiceChannel", channelId);
        }

        CurrentChannelId = null;
        lock (_lock)
        {
            _participants = [];
            _activeSpeakers = [];
            _participantMuteStates = [];
        }
        IsMuted = false;
        StateChanged?.Invoke();
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        StateChanged?.Invoke();

        // Apply the audio change and broadcast mute state asynchronously.
        // Both operations are best-effort: a failure does not roll back the local toggle.
        _ = Task.Run(async () =>
        {
            await SetMicrophoneEnabledAsync(!IsMuted);
            await BroadcastMuteStateAsync();
        });
    }

    // ------------------------------------------------------------------
    // LiveKit integration via JS interop
    // ------------------------------------------------------------------

    private async Task ConnectLiveKitAsync(string channelId)
    {
        try
        {
            var tokenResponse = await _http.GetFromJsonAsync<LiveKitTokenResponse>(
                $"api/voice/token/{Uri.EscapeDataString(channelId)}");

            if (tokenResponse is null)
                return;

            var module = await GetJsModuleAsync();
            if (module is null)
                return;

            _dotNetRef ??= DotNetObjectReference.Create(this);

            var connected = await module.InvokeAsync<bool>(
                "connect", tokenResponse.Url, tokenResponse.Token, _dotNetRef, !IsMuted);

            if (connected)
            {
                // Ensure LiveKit mic state matches IsMuted and broadcast so remote UI stays consistent.
                await SetMicrophoneEnabledAsync(!IsMuted);
                await BroadcastMuteStateAsync();
            }
        }
        catch
        {
            // LiveKit connection is best-effort; presence via SignalR still works.
        }
    }

    private async Task DisconnectLiveKitAsync()
    {
        try
        {
            var module = await GetJsModuleAsync();
            if (module is not null)
            {
                await module.InvokeVoidAsync("disconnect");
            }
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }

    private async Task SetMicrophoneEnabledAsync(bool enabled)
    {
        try
        {
            var module = await GetJsModuleAsync();
            if (module is not null)
            {
                await module.InvokeVoidAsync("setMicrophoneEnabled", enabled);
            }
        }
        catch
        {
            // Ignore errors – mute state is still tracked locally
        }
    }

    private async Task BroadcastMuteStateAsync()
    {
        try
        {
            if (IsConnected && CurrentChannelId is not null)
            {
                await _hubConnection!.InvokeAsync("UpdateMuteState", CurrentChannelId, IsMuted);
            }
        }
        catch
        {
            // Best-effort broadcast
        }
    }

    private async Task<IJSObjectReference?> GetJsModuleAsync()
    {
        if (_jsModule is not null)
            return _jsModule;

        try
        {
            _jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Vox.Shared.UI/voiceInterop.js");
        }
        catch
        {
            // JS interop may not be available (e.g., pre-rendering)
        }

        return _jsModule;
    }

    // ------------------------------------------------------------------
    // JSInvokable callbacks from voiceInterop.js
    // ------------------------------------------------------------------

    [JSInvokable]
    public void OnActiveSpeakersChanged(string[] speakerIds)
    {
        lock (_lock)
        {
            _activeSpeakers = new HashSet<string>(speakerIds);
        }
        StateChanged?.Invoke();
    }

    [JSInvokable]
    public void OnParticipantMuteChanged(string participantId, bool isMuted)
    {
        lock (_lock)
        {
            _participantMuteStates[participantId] = isMuted;
        }
        StateChanged?.Invoke();
    }

    [JSInvokable]
    public void OnLiveKitDisconnected()
    {
        lock (_lock)
        {
            _activeSpeakers = [];
        }
        StateChanged?.Invoke();
    }

    // ------------------------------------------------------------------
    // Disposal
    // ------------------------------------------------------------------

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            if (CurrentChannelId is not null && IsConnected)
            {
                try
                {
                    await DisconnectLiveKitAsync();
                    await _hubConnection.InvokeAsync("LeaveVoiceChannel", CurrentChannelId);
                }
                catch
                {
                    // Ignore errors during disposal
                }
            }

            await _hubConnection.DisposeAsync();
        }

        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.DisposeAsync();
            }
            catch
            {
                // Ignore errors during disposal
            }
        }

        _dotNetRef?.Dispose();
    }

    private sealed record LiveKitTokenResponse(string Token, string Url);
}
