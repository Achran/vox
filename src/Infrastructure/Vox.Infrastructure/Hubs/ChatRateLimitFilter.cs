using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace Vox.Infrastructure.Hubs;

public class ChatRateLimitFilter : IHubFilter
{
    private readonly ConcurrentDictionary<string, UserRateState> _rates = new();

    private const int PermitLimit = 10;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan CleanupThreshold = TimeSpan.FromSeconds(30);
    private DateTime _lastCleanup = DateTime.UtcNow;

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (invocationContext.HubMethodName == nameof(ChatHub.SendMessage))
        {
            var userId = invocationContext.Context.UserIdentifier ?? invocationContext.Context.ConnectionId;
            var now = DateTime.UtcNow;

            CleanupStaleEntries(now);

            var state = _rates.GetOrAdd(userId, _ => new UserRateState());

            lock (state)
            {
                if (now - state.WindowStart >= Window)
                {
                    state.WindowStart = now;
                    state.Count = 1;
                }
                else if (state.Count >= PermitLimit)
                {
                    throw new HubException("Rate limit exceeded. Please wait before sending more messages.");
                }
                else
                {
                    state.Count++;
                }
            }
        }

        return await next(invocationContext);
    }

    private void CleanupStaleEntries(DateTime now)
    {
        if (now - _lastCleanup < CleanupThreshold)
            return;

        _lastCleanup = now;

        foreach (var kvp in _rates)
        {
            if (now - kvp.Value.WindowStart >= CleanupThreshold)
            {
                _rates.TryRemove(kvp.Key, out _);
            }
        }
    }

    private sealed class UserRateState
    {
        public DateTime WindowStart = DateTime.UtcNow;
        public int Count;
    }
}
