using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Vox.Shared.UI.Auth;

namespace Vox.Shared.UI.Services;

public sealed class PresenceService : IPresenceService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStorageService _tokenStorage;
    private HubConnection? _hubConnection;
    private readonly List<OnlineUserInfo> _onlineUsers = [];
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public IReadOnlyList<OnlineUserInfo> OnlineUsers => _onlineUsers;
    public event Action? OnUsersChanged;

    public PresenceService(HttpClient httpClient, ITokenStorageService tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    public async Task StartAsync()
    {
        if (_hubConnection is not null)
            return;

        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
        var hubUrl = $"{baseUrl}/hubs/chat";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () => await _tokenStorage.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<JsonElement>("OnlineUsersList", users =>
        {
            _onlineUsers.Clear();
            foreach (var user in users.EnumerateArray())
            {
                var info = user.Deserialize<OnlineUserInfo>(JsonOptions);
                if (info is not null)
                    _onlineUsers.Add(info);
            }
            OnUsersChanged?.Invoke();
        });

        _hubConnection.On<JsonElement>("UserOnline", user =>
        {
            var info = user.Deserialize<OnlineUserInfo>(JsonOptions);
            if (info is not null && !_onlineUsers.Any(u => u.UserId == info.UserId))
            {
                _onlineUsers.Add(info);
                OnUsersChanged?.Invoke();
            }
        });

        _hubConnection.On<string>("UserOffline", userId =>
        {
            var removed = _onlineUsers.RemoveAll(u => u.UserId == userId);
            if (removed > 0)
                OnUsersChanged?.Invoke();
        });

        _hubConnection.Reconnected += async _ =>
        {
            await _hubConnection.InvokeAsync("GetOnlineUsers");
        };

        await _hubConnection.StartAsync();
        await _hubConnection.InvokeAsync("GetOnlineUsers");
    }

    public async Task StopAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
        _onlineUsers.Clear();
        OnUsersChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }
}
