using Microsoft.AspNetCore.SignalR;

namespace Vox.Infrastructure.Hubs;

public class ChatHub : Hub
{
    public async Task SendMessage(string channelId, string content)
    {
        await Clients.Group(channelId).SendAsync("ReceiveMessage", new
        {
            ChannelId = channelId,
            Content = content,
            AuthorId = Context.UserIdentifier,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public async Task JoinChannel(string channelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
        await Clients.Group(channelId).SendAsync("UserJoined", Context.UserIdentifier);
    }

    public async Task LeaveChannel(string channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
        await Clients.Group(channelId).SendAsync("UserLeft", Context.UserIdentifier);
    }
}
