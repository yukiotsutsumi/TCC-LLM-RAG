using Rag.Core.Domain.DTOs.Ingest.Requests;
using Rag.Core.Interfaces.Services;
using System.ComponentModel.DataAnnotations;

namespace Rag.App.Endpoints;

public static class IngestEndpoints
{
    public static IEndpointRouteBuilder MapIngestEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api")
                          .WithTags("Ingest");

        group.MapPost("/ingest-text", async (IngestTextBody body, IIngestionService ingestion, CancellationToken ct) =>
        {
            var dto = new IngestTextRequest(body.Title, body.Source, body.Text, body.Model);
            var result = await ingestion.IngestTextAsync(dto, ct);
            if (result.DocumentId == Guid.Empty)
                return Results.BadRequest("Texto obrigatório.");
            return Results.Ok(result);
        });

        group.MapPost("/upload", async (HttpRequest req, IIngestionService ingestion, CancellationToken ct) =>
        {
            if (!req.HasFormContentType || req.Form.Files.Count == 0)
                return Results.BadRequest("Envie um arquivo .txt em multipart/form-data");

            var file = req.Form.Files[0];
            if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest("Apenas .txt por enquanto.");

            using var reader = new StreamReader(file.OpenReadStream());
            var text = await reader.ReadToEndAsync(ct);

            var title = req.Form.TryGetValue("title", out var t) ? t.ToString() : file.FileName;
            var source = req.Form.TryGetValue("source", out var s) ? s.ToString() : "upload";
            var model = req.Form.TryGetValue("model", out var m) ? m.ToString() : null;

            var dto = new IngestTextRequest(title, source, text, model);
            var result = await ingestion.IngestTextAsync(dto, ct);
            return Results.Ok(result);
        });

        return routes;
    }

    public record IngestTextBody(
        [property: Required] string Title,
        [property: Required] string Source,
        [property: Required] string Text,
        string? Model
    );
}