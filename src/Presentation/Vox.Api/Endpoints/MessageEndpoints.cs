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
        Guid channelId, int? pageSize, DateTime? before, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetChannelMessagesQuery(channelId, pageSize ?? 50, before), ct);
        return Results.Ok(result);
    }
}
