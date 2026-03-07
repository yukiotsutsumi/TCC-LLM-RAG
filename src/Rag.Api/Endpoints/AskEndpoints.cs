using Rag.Core.Domain.DTOs.Ask.Requests;
using Rag.Core.Interfaces.Services;
using System.Diagnostics;
using System.Text.Json;

namespace Rag.Api.Endpoints;

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
        }).WithRequestTimeout(TimeSpan.FromMinutes(10))
        .RequireAuthorization();

        group.MapPost("/ask/stream", async (
            AskRequest req,
            IRagService rag,
            HttpResponse httpResponse,
            CancellationToken ct) =>
                {
                    httpResponse.ContentType = "application/x-ndjson";
                    httpResponse.Headers.CacheControl = "no-cache";

                    await foreach (var part in rag.AskStreamAsync(req, ct))
                    {
                        var json = JsonSerializer.Serialize(part);
                        await httpResponse.WriteAsync(json + "\n", ct);
                        await httpResponse.Body.FlushAsync(ct);
                    }
                })
        .WithRequestTimeout(TimeSpan.FromMinutes(10))
        .RequireAuthorization();

        return routes;
    }
}