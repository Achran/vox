using MediatR;
using Vox.Application.Features.Messages.Queries.GetChannelMessages;

namespace Vox.Api.Endpoints;

public static class MessageEndpoints
{
    public static IEndpointRouteBuilder MapMessageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/channels/{channelId:guid}/messages")
            .WithTags("Messages")
            .RequireAuthorization();

        group.MapGet("/", GetChannelMessagesAsync).WithName("GetChannelMessages").RequireRateLimiting("chat");

        return app;
    }

    private static async Task<IResult> GetChannelMessagesAsync(
        Guid channelId, int? pageSize, DateTimeOffset? before, IMediator mediator, CancellationToken ct)
    {
        var beforeUtc = before?.UtcDateTime;
        var result = await mediator.Send(
            new GetChannelMessagesQuery(channelId, pageSize ?? 50, beforeUtc), ct);
        return Results.Ok(result);
    }
}
