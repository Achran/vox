namespace Vox.Api.Endpoints;

public static class EndpointExtensions
{
    public static RouteGroupBuilder MapVoxEndpoints(this RouteGroupBuilder group)
    {
        group.MapHealthGroup();
        return group;
    }

    private static RouteGroupBuilder MapHealthGroup(this RouteGroupBuilder group)
    {
        group.MapGet("/ping", () => Results.Ok(new { Status = "healthy", Timestamp = DateTimeOffset.UtcNow }))
             .WithName("Ping")
             .WithSummary("Ping endpoint");
        return group;
    }
}
