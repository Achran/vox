using FluentValidation;
using MediatR;
using Vox.Application.Features.Channels.Commands.CreateChannel;
using Vox.Application.Features.Channels.Commands.DeleteChannel;
using Vox.Application.Features.Channels.Commands.UpdateChannel;
using Vox.Application.Features.Channels.Queries.GetChannel;
using Vox.Application.Features.Channels.Queries.GetServerChannels;

namespace Vox.Api.Endpoints;

public static class ChannelEndpoints
{
    public static IEndpointRouteBuilder MapChannelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/servers/{serverId:guid}/channels").WithTags("Channels").RequireAuthorization();

        group.MapPost("/", CreateChannelAsync).WithName("CreateChannel");
        group.MapGet("/", GetServerChannelsAsync).WithName("GetServerChannels");

        var channelGroup = app.MapGroup("/api/channels").WithTags("Channels").RequireAuthorization();

        channelGroup.MapGet("/{id:guid}", GetChannelByIdAsync).WithName("GetChannelById");
        channelGroup.MapPut("/{id:guid}", UpdateChannelAsync).WithName("UpdateChannel");
        channelGroup.MapDelete("/{id:guid}", DeleteChannelAsync).WithName("DeleteChannel");

        return app;
    }

    private static async Task<IResult> CreateChannelAsync(
        Guid serverId, CreateChannelRequest request, HttpContext httpContext, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var userId = EndpointHelpers.GetDomainUserId(httpContext);
            var result = await mediator.Send(
                new CreateChannelCommand(serverId, request.Name, request.Type, userId), ct);
            return Results.Created($"/api/channels/{result.Id}", result);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetServerChannelsAsync(
        Guid serverId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetServerChannelsQuery(serverId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetChannelByIdAsync(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetChannelByIdQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateChannelAsync(
        Guid id, UpdateChannelRequest request, HttpContext httpContext, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var userId = EndpointHelpers.GetDomainUserId(httpContext);
            var result = await mediator.Send(
                new UpdateChannelCommand(id, request.Name, userId), ct);
            return Results.Ok(result);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> DeleteChannelAsync(
        Guid id, HttpContext httpContext, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var userId = EndpointHelpers.GetDomainUserId(httpContext);
            await mediator.Send(new DeleteChannelCommand(id, userId), ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }
}

public sealed record CreateChannelRequest(string Name, string Type);
public sealed record UpdateChannelRequest(string Name);
