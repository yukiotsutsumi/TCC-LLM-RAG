namespace Rag.App.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/ping", () => Results.Ok(new { ok = true, time = DateTime.UtcNow }));
        return routes;
    }
}