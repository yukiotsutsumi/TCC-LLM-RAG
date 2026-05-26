using Rag.Core.Domain.DTOs.Documents.Response;
using Rag.Core.Interfaces.Repositories;

namespace Rag.Api.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/documents")
            .WithTags("Documents")
            .RequireAuthorization();

        group.MapGet("/", async (IDocumentRepository repo, CancellationToken ct) =>
        {
            var docs = await repo.GetAllAsync(ct);
            var result = docs.Select(d => new DocumentDto(
                d.Id,
                d.Title,
                d.Source,
                d.CreatedAt,
                d.Chunks.Count,
                d.Chunks.Count > 0 ? "ready" : "processing",
                d.AccessLevel
            ));
            return Results.Ok(result);
        });

        group.MapGet("/stats", async (IDocumentRepository repo, CancellationToken ct) =>
        {
            var docs        = await repo.GetAllAsync(ct);
            var totalChunks = await repo.GetTotalChunksAsync(ct);
            return Results.Ok(new DocumentStatsDto(docs.Count, totalChunks));
        });

        group.MapGet("/{id:guid}", async (Guid id, IDocumentRepository repo) =>
        {
            var doc = await repo.GetAsync(id);
            if (doc is null) return Results.NotFound();

            var dto = new DocumentDto(
                doc.Id,
                doc.Title,
                doc.Source,
                doc.CreatedAt,
                doc.Chunks.Count,
                doc.Chunks.Count > 0 ? "ready" : "processing",
                doc.AccessLevel
            );
            return Results.Ok(dto);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IDocumentRepository repo) =>
        {
            var deleted = await repo.DeleteAsync(id);
            return deleted
                ? Results.Ok(new DeleteDocumentResponse(true, "Documento removido com sucesso."))
                : Results.NotFound(new DeleteDocumentResponse(false, "Documento não encontrado."));
        });

        group.MapPut("/{id:guid}/access-level", async (Guid id, Rag.Core.Domain.Enums.DocumentAccessLevel level, IDocumentRepository repo, System.Security.Claims.ClaimsPrincipal user) =>
        {
            Guid? userId = null;
            var sub = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(sub) && Guid.TryParse(sub, out var g)) userId = g;

            var updated = await repo.UpdateAccessLevelAsync(id, level, userId);
            return updated
                ? Results.Ok(new { Success = true, Message = "Nível de acesso atualizado." })
                : Results.NotFound(new { Success = false, Message = "Documento não encontrado." });
        }).RequireAuthorization("AdminPolicy");

        return routes;
    }
}
