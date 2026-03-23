using MediatR;
using Vox.Application.Features.Presence.Queries.GetOnlineChannelUsers;
using Vox.Application.Features.Presence.Queries.GetOnlineServerUsers;

namespace Vox.Api.Endpoints;

public static class PresenceEndpoints
{
    public static IEndpointRouteBuilder MapPresenceEndpoints(this IEndpointRouteBuilder app)
    {
        var serverGroup = app.MapGroup("/api/servers/{serverId:guid}").WithTags("Presence").RequireAuthorization();
        serverGroup.MapGet("/online-users", GetOnlineServerUsersAsync).WithName("GetOnlineServerUsers");

        var channelGroup = app.MapGroup("/api/channels/{channelId:guid}").WithTags("Presence").RequireAuthorization();
        channelGroup.MapGet("/online-users", GetOnlineChannelUsersAsync).WithName("GetOnlineChannelUsers");

        return app;
    }

    private static async Task<IResult> GetOnlineServerUsersAsync(
        Guid serverId, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetOnlineServerUsersQuery(serverId), ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetOnlineChannelUsersAsync(
        Guid channelId, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetOnlineChannelUsersQuery(channelId), ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
