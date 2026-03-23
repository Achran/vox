using Microsoft.AspNetCore.SignalR;
using Vox.Application.Abstractions;

namespace Vox.Infrastructure.Hubs;

public class ChatHub : Hub
{
    private readonly IPresenceService _presenceService;

    public ChatHub(IPresenceService presenceService)
    {
        _presenceService = presenceService;
    }

    public async Task SendMessage(string channelId, string message)
    {
        await Clients.Group(channelId).SendAsync("ReceiveMessage", new
        {
            UserId = Context.UserIdentifier,
            ChannelId = channelId,
            Content = message,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task JoinChannel(string channelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
        await _presenceService.UserJoinedChannelAsync(Context.ConnectionId, channelId);
        await Clients.Group(channelId).SendAsync("UserJoined", Context.UserIdentifier, channelId);
        await Clients.Group(channelId).SendAsync("UserStatusChanged", Context.UserIdentifier, "Online");
    }

    public async Task LeaveChannel(string channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
        await _presenceService.UserLeftChannelAsync(Context.ConnectionId, channelId);
        await Clients.Group(channelId).SendAsync("UserLeft", Context.UserIdentifier, channelId);
    }

    public async Task Heartbeat()
    {
        await _presenceService.HeartbeatAsync(Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId is not null)
        {
            await _presenceService.UserConnectedAsync(Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        var channels = _presenceService.GetChannelsByConnectionId(Context.ConnectionId);

        await _presenceService.UserDisconnectedAsync(Context.ConnectionId);

        if (userId is not null)
        {
            foreach (var channelId in channels)
            {
                await Clients.Group(channelId).SendAsync("UserStatusChanged", userId, "Offline");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
