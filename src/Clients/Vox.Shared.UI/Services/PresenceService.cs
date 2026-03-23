using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Vox.Shared.UI.Auth;

namespace Vox.Shared.UI.Services;

public sealed class PresenceService : IPresenceService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStorageService _tokenStorage;
    private HubConnection? _hubConnection;
    private readonly object _lock = new();
    private List<OnlineUserInfo> _onlineUsers = [];
    private Guid? _serverId;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public IReadOnlyList<OnlineUserInfo> OnlineUsers
    {
        get { lock (_lock) { return _onlineUsers.ToList(); } }
    }

    public event Action? OnUsersChanged;

    public PresenceService(HttpClient httpClient, ITokenStorageService tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    public async Task StartAsync(Guid? serverId = null)
    {
        if (_hubConnection is not null)
            return;

        _serverId = serverId;

        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
        var hubUrl = $"{baseUrl}/hubs/chat";

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () => await _tokenStorage.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<JsonElement>("UserOnline", user =>
        {
            var info = user.Deserialize<OnlineUserInfo>(JsonOptions);
            if (info is not null)
            {
                lock (_lock)
                {
                    if (!_onlineUsers.Any(u => u.UserId == info.UserId))
                        _onlineUsers.Add(info);
                }
                OnUsersChanged?.Invoke();
            }
        });

        connection.On<string>("UserOffline", userId =>
        {
            bool changed;
            lock (_lock)
            {
                var before = _onlineUsers.Count;
                _onlineUsers = _onlineUsers.Where(u => u.UserId != userId).ToList();
                changed = _onlineUsers.Count != before;
            }
            if (changed)
                OnUsersChanged?.Invoke();
        });

        connection.Reconnected += async _ =>
        {
            await FetchOnlineUsersAsync();
        };

        try
        {
            await connection.StartAsync();
            _hubConnection = connection;
            await FetchOnlineUsersAsync();
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task StopAsync()
    {
        lock (_lock) { _onlineUsers = []; }
        OnUsersChanged?.Invoke();

        if (_hubConnection is not null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private async Task FetchOnlineUsersAsync()
    {
        if (_serverId is null) return;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/servers/{_serverId}/online-users");
            var accessToken = await _tokenStorage.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(accessToken))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var dtos = await response.Content.ReadFromJsonAsync<List<PresenceUserDto>>(JsonOptions) ?? [];
                lock (_lock)
                {
                    _onlineUsers = dtos.Select(d => new OnlineUserInfo(d.UserId, d.DisplayName ?? d.UserId)).ToList();
                }
                OnUsersChanged?.Invoke();
            }
        }
        catch
        {
            // Silently ignore fetch failures; live events will still update the list
        }
    }

    private sealed record PresenceUserDto(string UserId, string? Status, string? DisplayName);
}
