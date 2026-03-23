namespace Vox.Api.Endpoints;

internal static class EndpointHelpers
{
    internal static Guid GetDomainUserId(HttpContext httpContext)
    {
        var domainUserId = httpContext.User.FindFirst("domain_user_id")?.Value;
        if (domainUserId is null || !Guid.TryParse(domainUserId, out var userId))
        {
            throw new UnauthorizedAccessException("Domain user ID not found in token.");
        }
        return userId;
    }
}
