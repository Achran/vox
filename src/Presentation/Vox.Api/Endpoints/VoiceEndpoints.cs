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

        return app;
    }

    private static IResult GetLiveKitTokenAsync(
        Guid channelId, HttpContext httpContext, ILiveKitService liveKitService)
    {
        var userId = httpContext.User.FindFirst("domain_user_id")?.Value
                     ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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
}
