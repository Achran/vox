namespace Vox.Api.Endpoints;

internal static class EndpointHelpers
{
    internal static bool TryGetDomainUserId(HttpContext httpContext, out Guid userId)
    {
        var domainUserId = httpContext.User.FindFirst("domain_user_id")?.Value;
        if (domainUserId is not null && Guid.TryParse(domainUserId, out userId))
        {
            return true;
        }
        userId = Guid.Empty;
        return false;
    }
}
