using System.Collections.Concurrent;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Vox.Application.Features.Messages.Commands.SendMessage;

namespace Vox.Infrastructure.Hubs;

public class ChatHub : Hub
{
    private readonly IMediator _mediator;

    private static readonly ConcurrentDictionary<string, OnlineUserEntry> _onlineUsers = new();

    public ChatHub(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task SendMessage(string channelId, string message)
    {
        var authorId = GetDomainUserId();

        if (!Guid.TryParse(channelId, out var channelGuid))
        {
            throw new HubException("Invalid channel ID.");
        }

        var result = await _mediator.Send(new SendMessageCommand(channelGuid, message, authorId));

        await Clients.Group(channelId).SendAsync("ReceiveMessage", new
        {
            result.Id,
            UserId = result.AuthorId,
            result.ChannelId,
            result.Content,
            Timestamp = result.CreatedAt
        });
    }

    public async Task JoinChannel(string channelId)
    {
        var userId = GetDomainUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
        await Clients.Group(channelId).SendAsync("UserJoined", userId, channelId);
    }

    public async Task LeaveChannel(string channelId)
    {
        var userId = GetDomainUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
        await Clients.Group(channelId).SendAsync("UserLeft", userId, channelId);
    }

    public async Task GetOnlineUsers()
    {
        var users = _onlineUsers.Values
            .Select(u => new { u.UserId, u.DisplayName })
            .ToList();
        await Clients.Caller.SendAsync("OnlineUsersList", users);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = TryGetDomainUserId();
        if (userId is not null)
        {
            var displayName = Context.User?.FindFirst("display_name")?.Value
                              ?? Context.User?.FindFirst("unique_name")?.Value
                              ?? "User";

            var entry = new OnlineUserEntry(userId.Value.ToString(), displayName);
            _onlineUsers[Context.ConnectionId] = entry;

            await Clients.Others.SendAsync("UserOnline", new { entry.UserId, entry.DisplayName });
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_onlineUsers.TryRemove(Context.ConnectionId, out var entry))
        {
            var stillOnline = _onlineUsers.Values.Any(u => u.UserId == entry.UserId);
            if (!stillOnline)
            {
                await Clients.Others.SendAsync("UserOffline", entry.UserId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetDomainUserId()
    {
        var claim = Context.User?.FindFirst("domain_user_id")?.Value;
        if (claim is null || !Guid.TryParse(claim, out var userId))
        {
            throw new HubException("Unauthorized.");
        }
        return userId;
    }

    private Guid? TryGetDomainUserId()
    {
        var claim = Context.User?.FindFirst("domain_user_id")?.Value;
        if (claim is not null && Guid.TryParse(claim, out var userId))
        {
            return userId;
        }
        return null;
    }

    private sealed record OnlineUserEntry(string UserId, string DisplayName);
}
