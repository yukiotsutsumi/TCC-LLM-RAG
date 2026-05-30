using Rag.Core.Domain.DTOs.Ingest.Requests;
using Rag.Core.Interfaces.Services;
using System.ComponentModel.DataAnnotations;

namespace Rag.Api.Endpoints;

public static class IngestEndpoints
{
    public static IEndpointRouteBuilder MapIngestEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api").WithTags("Ingest");

        group.MapPost("/ingest-text", async (
            IngestTextBody body,
            IIngestionService ingestion,
            CancellationToken ct) =>
        {
            var dto = new IngestTextRequest(body.Title, body.Source, body.Text, body.Model);
            var result = await ingestion.IngestTextAsync(dto, ct);
            if (result.DocumentId == Guid.Empty)
                return Results.BadRequest("Texto obrigatório.");
            return Results.Ok(result);
        }).RequireAuthorization();

        group.MapPost("/upload", async (
            HttpRequest req,
            IIngestionService ingestion,
            CancellationToken ct) =>
        {
            if (!req.HasFormContentType || req.Form.Files.Count == 0)
                return Results.BadRequest("Envie um arquivo em multipart/form-data.");

            var file = req.Form.Files[0];
            var ext  = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (ext != ".txt" && ext != ".pdf")
                return Results.BadRequest("Apenas arquivos .txt e .pdf são aceitos.");

            var title  = req.Form.TryGetValue("title",  out var t) ? t.ToString() : Path.GetFileNameWithoutExtension(file.FileName);
            var source = req.Form.TryGetValue("source", out var s) ? s.ToString() : "upload";
            var model  = req.Form.TryGetValue("model",  out var m) ? m.ToString() : null;

            string text;

            if (ext == ".pdf")
            {
                using var pdfStream = file.OpenReadStream();
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(pdfStream);
                var pages = pdf.GetPages()
                    .Select(p => string.Join(" ", p.GetWords().Select(w => w.Text)));
                text = string.Join("\n", pages);

                if (string.IsNullOrWhiteSpace(text))
                    return Results.BadRequest("Não foi possível extrair texto do PDF.");
            }
            else
            {
                using var reader = new StreamReader(file.OpenReadStream());
                text = await reader.ReadToEndAsync(ct);
            }

            var dto    = new IngestTextRequest(title, source, text, model);
            var result = await ingestion.IngestTextAsync(dto, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .DisableAntiforgery();

        return routes;
    }

    public record IngestTextBody(
        [property: Required] string Title,
        [property: Required] string Source,
        [property: Required] string Text,
        string? Model
    );
}
