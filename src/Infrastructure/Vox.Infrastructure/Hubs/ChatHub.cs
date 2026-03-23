using MediatR;
using Microsoft.AspNetCore.SignalR;
using Vox.Application.Features.Messages.Commands.SendMessage;

namespace Vox.Infrastructure.Hubs;

public class ChatHub : Hub
{
    private readonly IMediator _mediator;

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

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
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
