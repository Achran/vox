using Microsoft.AspNetCore.SignalR.Client;
using Vox.Shared.UI.Auth;

namespace Vox.Shared.UI.Services;

public sealed class ChatService : IChatService
{
    private readonly ITokenStorageService _tokenStorage;
    private readonly Uri _hubUrl;
    private HubConnection? _hubConnection;
    private readonly HashSet<string> _joinedChannels = new();

    public event Action<MessageResponse>? MessageReceived;
    public event Action<string, string>? UserTyping;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public ChatService(HttpClient http, ITokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage;

        if (http.BaseAddress is null)
        {
            throw new InvalidOperationException(
                "HttpClient.BaseAddress must be configured before creating ChatService.");
        }

        _hubUrl = new Uri(http.BaseAddress, "hubs/chat");
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection is not null && _hubConnection.State != HubConnectionState.Disconnected)
            return;

        // Dispose the previous connection if it exists (e.g. after a failed StartAsync)
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

        _hubConnection.On<MessageResponse>("ReceiveMessage", message =>
        {
            MessageReceived?.Invoke(message);
        });

        _hubConnection.On<string, string>("UserTyping", (userId, channelId) =>
        {
            UserTyping?.Invoke(userId, channelId);
        });

        _hubConnection.Reconnected += async _ =>
        {
            // Rejoin all tracked channels after reconnect
            foreach (var channelId in _joinedChannels)
            {
                await _hubConnection.InvokeAsync("JoinChannel", channelId);
            }
        };

        await _hubConnection.StartAsync();
    }

    public async Task JoinChannelAsync(string channelId)
    {
        _joinedChannels.Add(channelId);
        if (IsConnected)
        {
            await _hubConnection!.InvokeAsync("JoinChannel", channelId);
        }
    }

    public async Task LeaveChannelAsync(string channelId)
    {
        _joinedChannels.Remove(channelId);
        if (IsConnected)
        {
            await _hubConnection!.InvokeAsync("LeaveChannel", channelId);
        }
    }

    public async Task SendMessageAsync(string channelId, string message)
    {
        if (IsConnected)
        {
            await _hubConnection!.InvokeAsync("SendMessage", channelId, message);
        }
    }

    public async Task SendTypingAsync(string channelId)
    {
        if (IsConnected)
        {
            await _hubConnection!.InvokeAsync("StartTyping", channelId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}

