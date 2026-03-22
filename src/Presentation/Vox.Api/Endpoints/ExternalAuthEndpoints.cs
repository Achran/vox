using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Vox.Application.Features.Auth.Commands.ExternalLogin;
using Vox.Infrastructure.DependencyInjection;
using Vox.Infrastructure.Identity;

namespace Vox.Api.Endpoints;

public static class ExternalAuthEndpoints
{
    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Google", "Microsoft", "Facebook", "Twitter", "GitHub", "Steam"
    };

    public static IEndpointRouteBuilder MapExternalAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth/external").WithTags("ExternalAuth");

        group.MapGet("/{provider}", ChallengeAsync)
            .WithName("ExternalChallenge")
            .AllowAnonymous();

        group.MapGet("/{provider}/callback", CallbackAsync)
            .WithName("ExternalCallback")
            .AllowAnonymous();

        return app;
    }

    private static IResult ChallengeAsync(
        string provider,
        HttpContext httpContext)
    {
        if (!SupportedProviders.Contains(provider))
        {
            return Results.BadRequest(new { error = $"Provider '{provider}' is not supported." });
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = $"/api/auth/external/{provider}/callback",
            Items = { { "LoginProvider", provider } }
        };

        return Results.Challenge(properties, [provider]);
    }

    private static async Task<IResult> CallbackAsync(
        string provider,
        HttpContext httpContext,
        IMediator mediator,
        IOptions<ExternalAuthSettings> externalAuthOptions,
        CancellationToken ct)
    {
        var result = await httpContext.AuthenticateAsync(
            InfrastructureServiceExtensions.ExternalAuthCookieScheme);

        if (!result.Succeeded || result.Principal is null)
        {
            return Results.Unauthorized();
        }

        var providerKey = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(providerKey))
        {
            return Results.BadRequest(new { error = "External login did not provide a user identifier." });
        }

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var displayName = result.Principal.FindFirstValue(ClaimTypes.Name)
            ?? result.Principal.FindFirstValue(ClaimTypes.GivenName);

        // Clean up the external cookie
        await httpContext.SignOutAsync(InfrastructureServiceExtensions.ExternalAuthCookieScheme);

        try
        {
            var tokens = await mediator.Send(
                new ExternalLoginCommand(provider, providerKey, email, displayName), ct);

            var settings = externalAuthOptions.Value;
            var callbackUrl = BuildClientCallbackUrl(settings.ClientCallbackUrl, tokens.AccessToken,
                tokens.RefreshToken, tokens.AccessTokenExpiresAt);

            return Results.Redirect(callbackUrl);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static string BuildClientCallbackUrl(
        string baseUrl,
        string accessToken,
        string refreshToken,
        DateTime expiresAt)
    {
        var separator = baseUrl.Contains('?') ? '&' : '?';
        return $"{baseUrl}{separator}access_token={Uri.EscapeDataString(accessToken)}" +
               $"&refresh_token={Uri.EscapeDataString(refreshToken)}" +
               $"&expires_at={Uri.EscapeDataString(expiresAt.ToString("O"))}";
    }
}
