using Rag.Core.Domain.DTOs.Ask.Requests;
using Rag.Core.Domain.Enums;
using Rag.Core.Interfaces.Services;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace Rag.Api.Endpoints;

public static class AskEndpoints
{
    public static IEndpointRouteBuilder MapAskEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api").WithTags("Ask");

        group.MapPost("/ask/stream", async (
            AskRequest req,
            IRagService rag,
            ClaimsPrincipal user,
            HttpResponse httpResponse,
            CancellationToken ct) =>
        {
            httpResponse.ContentType = "application/x-ndjson";
            httpResponse.Headers.CacheControl = "no-cache";

            var accessLevel = user.IsInRole("Admin")
                ? DocumentAccessLevel.Admin
                : DocumentAccessLevel.User;

            var roles = user.Claims
                .Where(c => c.Type.Contains("role", StringComparison.OrdinalIgnoreCase))
                .Select(c => $"{c.Type}={c.Value}")
                .ToList();

            await foreach (var part in rag.AskStreamAsync(req, accessLevel, ct))
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