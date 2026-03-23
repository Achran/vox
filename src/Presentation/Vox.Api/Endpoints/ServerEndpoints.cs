using FluentValidation;
using MediatR;
using Vox.Application.Features.Servers.Commands.CreateServer;
using Vox.Application.Features.Servers.Commands.DeleteServer;
using Vox.Application.Features.Servers.Commands.JoinServer;
using Vox.Application.Features.Servers.Commands.UpdateServer;
using Vox.Application.Features.Servers.Queries.GetServer;
using Vox.Application.Features.Servers.Queries.GetUserServers;

namespace Vox.Api.Endpoints;

public static class ServerEndpoints
{
    public static IEndpointRouteBuilder MapServerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/servers").WithTags("Servers").RequireAuthorization();

        group.MapPost("/", CreateServerAsync).WithName("CreateServer");
        group.MapGet("/{id:guid}", GetServerByIdAsync).WithName("GetServerById");
        group.MapGet("/", GetUserServersAsync).WithName("GetUserServers");
        group.MapPut("/{id:guid}", UpdateServerAsync).WithName("UpdateServer");
        group.MapDelete("/{id:guid}", DeleteServerAsync).WithName("DeleteServer");
        group.MapPost("/{id:guid}/join", JoinServerAsync).WithName("JoinServer");

        return app;
    }

    private static async Task<IResult> CreateServerAsync(
        CreateServerRequest request, HttpContext httpContext, IMediator mediator, CancellationToken ct)
    {
        if (!EndpointHelpers.TryGetDomainUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var result = await mediator.Send(
                new CreateServerCommand(request.Name, request.Description, userId), ct);
            return Results.Created($"/api/servers/{result.Id}", result);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
    }

    private static async Task<IResult> GetServerByIdAsync(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetServerByIdQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetUserServersAsync(
        HttpContext httpContext, IMediator mediator, CancellationToken ct)
    {
        if (!EndpointHelpers.TryGetDomainUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await mediator.Send(new GetUserServersQuery(userId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateServerAsync(
        Guid id, UpdateServerRequest request, HttpContext httpContext, IMediator mediator, CancellationToken ct)
    {
        if (!EndpointHelpers.TryGetDomainUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var result = await mediator.Send(
                new UpdateServerCommand(id, request.Name, request.Description, userId), ct);
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

    private static async Task<IResult> DeleteServerAsync(
        Guid id, HttpContext httpContext, IMediator mediator, CancellationToken ct)
    {
        if (!EndpointHelpers.TryGetDomainUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            await mediator.Send(new DeleteServerCommand(id, userId), ct);
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

    private static async Task<IResult> JoinServerAsync(
        Guid id, HttpContext httpContext, IMediator mediator, CancellationToken ct)
    {
        if (!EndpointHelpers.TryGetDomainUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var result = await mediator.Send(new JoinServerCommand(id, userId), ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }
}

public sealed record CreateServerRequest(string Name, string? Description);
public sealed record UpdateServerRequest(string Name, string? Description);
