using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vox.Application.Abstractions;
using Vox.Infrastructure.Hubs;

namespace Vox.Infrastructure.Services;

public sealed class PresenceHeartbeatService : BackgroundService
{
    private static readonly TimeSpan StaleTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(10);

    private readonly IPresenceService _presenceService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<PresenceHeartbeatService> _logger;

    public PresenceHeartbeatService(
        IPresenceService presenceService,
        IHubContext<ChatHub> hubContext,
        ILogger<PresenceHeartbeatService> logger)
    {
        _presenceService = presenceService;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
                await CleanupStaleConnectionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during presence heartbeat cleanup");
            }
        }
    }

    internal async Task CleanupStaleConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var staleConnectionIds = _presenceService.GetStaleConnectionIds(StaleTimeout);

        foreach (var connectionId in staleConnectionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Re-check: the connection may have sent a heartbeat after the snapshot
            if (!_presenceService.IsConnectionStale(connectionId, StaleTimeout))
            {
                continue;
            }

            var userId = _presenceService.GetUserIdByConnectionId(connectionId);
            var channels = _presenceService.GetChannelsByConnectionId(connectionId);

            await _presenceService.UserDisconnectedAsync(connectionId);

            if (userId is not null)
            {
                _logger.LogInformation(
                    "Stale connection detected for user {UserId}, connection {ConnectionId}",
                    userId,
                    connectionId);

                // Per-channel: notify if user no longer has presence in that channel
                foreach (var channelId in channels)
                {
                    if (!_presenceService.IsUserInChannel(userId, channelId))
                    {
                        await _hubContext.Clients.Group(channelId)
                            .SendAsync("UserStatusChanged", userId, "Offline", cancellationToken);
                    }
                }
            }
        }
    }
}
