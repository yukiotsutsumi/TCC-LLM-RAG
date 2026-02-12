using Rag.Core.Domain.DTOs.Ask.Requests;
using Rag.Core.Interfaces.Services;
using System.Diagnostics;

namespace Rag.App.Endpoints;

public static class AskEndpoints
{
    public static IEndpointRouteBuilder MapAskEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api").WithTags("Ask");

        group.MapPost("/ask", async (AskRequest req, IRagService rag) =>
        {
            var sw = Stopwatch.StartNew();
            var res = await rag.AskAsync(req);
            res.TookMs = (int)sw.ElapsedMilliseconds;
            return Results.Ok(res);
        }).WithRequestTimeout(TimeSpan.FromMinutes(10));

        return routes;
    }
}