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
        var userIdClaim = Context.User?.FindFirst("domain_user_id")?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var authorId))
        {
            throw new HubException("Unauthorized.");
        }

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
