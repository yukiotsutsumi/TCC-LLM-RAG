using Rag.Core.Domain.DTOs;
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
        }).WithRequestTimeout(TimeSpan.FromMinutes(5));

        group.MapPost("/ask-demo", (AskRequest req) =>
        {
            var answer = $"[demo] Você perguntou: \"{req.Question}\". K={req.K}, MaxCtx={req.MaxContextTokens}.";
            return Results.Ok(new AskResponse
            {
                Answer = answer,
                Sources = [],
                TookMs = 1
            });
        });

        return routes;
    }
}