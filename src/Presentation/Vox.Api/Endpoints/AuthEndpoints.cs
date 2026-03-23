using FluentValidation;
using MediatR;
using Vox.Application.Features.Auth.Commands.Login;
using Vox.Application.Features.Auth.Commands.RefreshToken;
using Vox.Application.Features.Auth.Commands.Register;
using Vox.Application.Features.Auth.Commands.RevokeToken;

namespace Vox.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous();

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .AllowAnonymous();

        group.MapPost("/revoke", RevokeAsync)
            .WithName("RevokeToken")
            .RequireAuthorization();

        group.MapGet("/me", MeAsync)
            .WithName("Me")
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new RegisterCommand(request.UserName, request.Email, request.DisplayName, request.Password), ct);
            return Results.Ok(result);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new LoginCommand(request.Email, request.Password), ct);
            return Results.Ok(result);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
    }

    private static async Task<IResult> RefreshAsync(RefreshRequest request, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new RefreshTokenCommand(request.RefreshToken), ct);
            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
    }

    private static async Task<IResult> RevokeAsync(RevokeRequest request, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new RevokeTokenCommand(request.RefreshToken), ct);
        return Results.NoContent();
    }

    private static IResult MeAsync(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value;
        var email = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? httpContext.User.FindFirst("email")?.Value;
        var userName = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? httpContext.User.FindFirst("unique_name")?.Value;
        var displayName = httpContext.User.FindFirst("display_name")?.Value;

        return Results.Ok(new { userId, email, userName, displayName });
    }
}

public sealed record RegisterRequest(string UserName, string Email, string DisplayName, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record RevokeRequest(string RefreshToken);
