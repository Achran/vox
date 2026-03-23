using Microsoft.AspNetCore.SignalR.Client;
using Vox.Shared.UI.Auth;

namespace Vox.Shared.UI.Services;

public sealed class ChatService : IChatService
{
    private readonly ITokenStorageService _tokenStorage;
    private readonly Uri _hubUrl;
    private HubConnection? _hubConnection;

    public event Action<MessageResponse>? MessageReceived;
    public event Action<string, string>? UserTyping;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public ChatService(HttpClient http, ITokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage;
        var baseUri = http.BaseAddress ?? new Uri("https://localhost");
        _hubUrl = new Uri(baseUri, "hubs/chat");
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection is not null && _hubConnection.State != HubConnectionState.Disconnected)
            return;

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

        await _hubConnection.StartAsync();
    }

    public async Task JoinChannelAsync(string channelId)
    {
        if (IsConnected)
        {
            await _hubConnection!.InvokeAsync("JoinChannel", channelId);
        }
    }

    public async Task LeaveChannelAsync(string channelId)
    {
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

