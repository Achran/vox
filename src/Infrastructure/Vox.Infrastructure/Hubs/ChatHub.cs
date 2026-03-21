using Microsoft.AspNetCore.SignalR;

namespace Vox.Infrastructure.Hubs;

public class ChatHub : Hub
{
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
        await Clients.Group(channelId).SendAsync("UserJoined", Context.UserIdentifier, channelId);
    }

    public async Task LeaveChannel(string channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
        await Clients.Group(channelId).SendAsync("UserLeft", Context.UserIdentifier, channelId);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
