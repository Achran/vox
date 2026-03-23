using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Vox.Application.Abstractions;

namespace Vox.Api.Endpoints;

public static class VoiceEndpoints
{
    public static IEndpointRouteBuilder MapVoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/voice")
            .WithTags("Voice")
            .RequireAuthorization();

        group.MapGet("/token/{channelId:guid}", GetLiveKitTokenAsync)
            .WithName("GetVoiceToken");

        group.MapPost("/rooms/{channelId:guid}", CreateRoomAsync)
            .WithName("CreateVoiceRoom");

        group.MapDelete("/rooms/{channelId:guid}", DeleteRoomAsync)
            .WithName("DeleteVoiceRoom");

        return app;
    }

    private static IResult GetLiveKitTokenAsync(
        Guid channelId, HttpContext httpContext, ILiveKitService liveKitService)
    {
        // Use ClaimTypes.NameIdentifier (sub) as the primary ID to align with
        // SignalR's default Context.UserIdentifier, ensuring LiveKit token identity
        // matches the IDs used by VoiceHub for peer signaling.
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var displayName = httpContext.User.FindFirst("display_name")?.Value
                          ?? httpContext.User.FindFirst("unique_name")?.Value
                          ?? "User";

        var roomName = $"voice-{channelId}";
        var token = liveKitService.GenerateToken(userId, displayName, roomName);

        var url = liveKitService.GetServerUrl();

        return Results.Ok(new { Token = token, Url = url });
    }

    private static async Task<IResult> CreateRoomAsync(
        Guid channelId, HttpContext httpContext, ILiveKitService liveKitService)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var roomName = $"voice-{channelId}";
        var created = await liveKitService.CreateRoomAsync(roomName);
        return Results.Ok(new { Room = created });
    }

    private static async Task<IResult> DeleteRoomAsync(
        Guid channelId, HttpContext httpContext, ILiveKitService liveKitService)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var roomName = $"voice-{channelId}";
        await liveKitService.DeleteRoomAsync(roomName);
        return Results.NoContent();
    }
}
