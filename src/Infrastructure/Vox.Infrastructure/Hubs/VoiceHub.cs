using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Vox.Application.Abstractions;

namespace Vox.Infrastructure.Hubs;

[Authorize]
public class VoiceHub : Hub
{
    private readonly IVoiceSessionService _voiceSessionService;

    public VoiceHub(IVoiceSessionService voiceSessionService)
    {
        _voiceSessionService = voiceSessionService;
    }

    /// <summary>Join a voice channel. Notifies other participants and returns current participant list.</summary>
    public async Task JoinVoiceChannel(string channelId)
    {
        var userId = GetUserId();
        var isNew = _voiceSessionService.JoinChannel(channelId, userId, Context.ConnectionId);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"voice:{channelId}");

        if (isNew)
        {
            await Clients.OthersInGroup($"voice:{channelId}").SendAsync("UserJoinedVoice", userId, channelId);
        }

        var participants = _voiceSessionService.GetParticipants(channelId);
        await Clients.Caller.SendAsync("VoiceParticipants", channelId, participants);
    }

    /// <summary>Leave a voice channel. Notifies remaining participants.</summary>
    public async Task LeaveVoiceChannel(string channelId)
    {
        var userId = GetUserId();
        var wasLast = _voiceSessionService.LeaveChannel(channelId, userId, Context.ConnectionId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"voice:{channelId}");

        if (wasLast)
        {
            await Clients.Group($"voice:{channelId}").SendAsync("UserLeftVoice", userId, channelId);
        }
    }

    /// <summary>Relay an SDP offer to a specific peer in the voice channel.</summary>
    public async Task SendOffer(string targetUserId, string channelId, string sdp)
    {
        var senderId = GetUserId();
        await Clients.User(targetUserId).SendAsync("ReceiveOffer", senderId, channelId, sdp);
    }

    /// <summary>Relay an SDP answer to a specific peer in the voice channel.</summary>
    public async Task SendAnswer(string targetUserId, string channelId, string sdp)
    {
        var senderId = GetUserId();
        await Clients.User(targetUserId).SendAsync("ReceiveAnswer", senderId, channelId, sdp);
    }

    /// <summary>Relay an ICE candidate to a specific peer in the voice channel.</summary>
    public async Task SendIceCandidate(string targetUserId, string channelId, string candidate)
    {
        var senderId = GetUserId();
        await Clients.User(targetUserId).SendAsync("ReceiveIceCandidate", senderId, channelId, candidate);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        var leftChannels = _voiceSessionService.RemoveConnection(Context.ConnectionId);

        if (userId is not null)
        {
            foreach (var channelId in leftChannels)
            {
                if (!_voiceSessionService.IsUserInVoiceChannel(channelId, userId))
                {
                    await Clients.Group($"voice:{channelId}").SendAsync("UserLeftVoice", userId, channelId);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private string GetUserId()
    {
        var userId = Context.UserIdentifier;
        if (userId is null)
        {
            throw new HubException("Unauthorized.");
        }
        return userId;
    }
}
