using System.Security.Claims;
using MediatR;
using Vox.Application.Features.Auth.Commands.LinkProvider;
using Vox.Application.Features.Auth.Commands.UnlinkProvider;
using Vox.Application.Features.Auth.Queries.GetLinkedProviders;

namespace Vox.Api.Endpoints;

public static class AccountLinkEndpoints
{
    public static IEndpointRouteBuilder MapAccountLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth/account-links")
            .WithTags("AccountLinks")
            .RequireAuthorization();

        group.MapGet("/", GetLinkedProvidersAsync)
            .WithName("GetLinkedProviders");

        group.MapPost("/", LinkProviderAsync)
            .WithName("LinkProvider");

        group.MapDelete("/{provider}", UnlinkProviderAsync)
            .WithName("UnlinkProvider");

        return app;
    }

    private static async Task<IResult> GetLinkedProvidersAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId is null) return Results.Unauthorized();

        var result = await mediator.Send(new GetLinkedProvidersQuery(userId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> LinkProviderAsync(
        LinkProviderRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId is null) return Results.Unauthorized();

        try
        {
            await mediator.Send(
                new LinkProviderCommand(userId, request.Provider, request.ProviderKey), ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UnlinkProviderAsync(
        string provider,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId is null) return Results.Unauthorized();

        try
        {
            await mediator.Send(new UnlinkProviderCommand(userId, provider), ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static string? GetUserId(HttpContext httpContext)
        => httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? httpContext.User.FindFirstValue("sub");
}

public sealed record LinkProviderRequest(string Provider, string ProviderKey);
