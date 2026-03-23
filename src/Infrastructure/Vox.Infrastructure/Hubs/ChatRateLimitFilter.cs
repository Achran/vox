using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace Vox.Infrastructure.Hubs;

public class ChatRateLimitFilter : IHubFilter
{
    private static readonly ConcurrentDictionary<string, UserRateState> _rates = new();

    private const int PermitLimit = 10;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(10);

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (invocationContext.HubMethodName == nameof(ChatHub.SendMessage))
        {
            var userId = invocationContext.Context.UserIdentifier ?? invocationContext.Context.ConnectionId;
            var now = DateTime.UtcNow;

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

    private sealed class UserRateState
    {
        public DateTime WindowStart = DateTime.UtcNow;
        public int Count;
    }
}
