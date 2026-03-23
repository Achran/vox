using MediatR;
using Microsoft.AspNetCore.SignalR;
using Vox.Application.Abstractions;
using Vox.Application.Features.Messages.Commands.SendMessage;

namespace Vox.Infrastructure.Hubs;

public class ChatHub : Hub
{
    private readonly IPresenceService _presenceService;
    private readonly IMediator _mediator;

    public ChatHub(IPresenceService presenceService, IMediator mediator)
    {
        _presenceService = presenceService;
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
            result.AuthorId,
            result.ChannelId,
            result.Content,
            result.IsEdited,
            result.CreatedAt
        });
    }

    public async Task JoinChannel(string channelId)
    {
        var userId = Context.UserIdentifier;
        var wasAlreadyInChannel = userId is not null && _presenceService.IsUserInChannel(userId, channelId);

        await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
        await _presenceService.UserJoinedChannelAsync(Context.ConnectionId, channelId);
        await Clients.Group(channelId).SendAsync("UserJoined", userId, channelId);

        if (!wasAlreadyInChannel)
        {
            await Clients.Group(channelId).SendAsync("UserStatusChanged", userId, "Online");
        }
    }

    public async Task LeaveChannel(string channelId)
    {
        var userId = GetDomainUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
        await _presenceService.UserLeftChannelAsync(Context.ConnectionId, channelId);
        await Clients.Group(channelId).SendAsync("UserLeft", Context.UserIdentifier, channelId);
    }

    public async Task StartTyping(string channelId)
    {
        var userId = GetDomainUserId();
        await Clients.OthersInGroup(channelId).SendAsync("UserTyping", userId.ToString(), channelId);
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
            // Per-channel: notify if user no longer has presence in that channel
            foreach (var channelId in channels)
            {
                if (!_presenceService.IsUserInChannel(userId, channelId))
                {
                    await Clients.Group(channelId).SendAsync("UserStatusChanged", userId, "Offline");
                }
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
}
