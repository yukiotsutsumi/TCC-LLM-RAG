using Rag.Core.Domain.DTOs.Ingest.Requests;
using Rag.Core.Domain.DTOs.Ingest.Responses;
using Rag.Core.Domain.Entities;
using Rag.Core.Interfaces;
using Rag.Core.Interfaces.Repositories;
using Rag.Core.Interfaces.Services;

namespace Rag.Infrastructure.Services;

public class IngestionService(
    IDocumentRepository docs,
    IChunkRepository chunkRepo,
    IChunker chunker,
    IOllamaClient ollama) : IIngestionService
{
    public async Task<IngestTextResponse> IngestTextAsync(IngestTextRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return new IngestTextResponse(Guid.Empty, 0);

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Source = request.Source,
            CreatedAt = DateTime.UtcNow
        };
        await docs.InsertAsync(doc);

        // Audit: record ingest action
        var audit = new Rag.Core.Domain.Entities.DocumentAuditLog
        {
            DocumentId = doc.Id,
            Action = Rag.Core.Domain.Enums.DocumentAction.Ingest,
            Details = $"Ingested document '{doc.Title}' source='{doc.Source}'",
            PerformedByUserId = null,
            PerformedAt = DateTime.UtcNow
        };
        await docs.AddDocumentAuditAsync(audit, ct);

        var split = chunker.Split(request.Text).ToList();
        if (split.Count == 0)
            return new IngestTextResponse(doc.Id, 0);

        var model = string.IsNullOrWhiteSpace(request.Model) ? "mxbai-embed-large" : request.Model!;
        var entities = new List<Chunk>(split.Count);

        foreach (var part in split)
        {
            var embedding = await ollama.EmbedAsync(model, part.Content, ct);

            entities.Add(new Chunk
            {
                Id = Guid.NewGuid(),
                DocumentId = doc.Id,
                Content = part.Content,
                Embedding = embedding,
                CreatedAt = DateTime.UtcNow,
                ChunkIndex = part.Index
            });
        }

        await chunkRepo.InsertManyAsync(entities, ct);

        return new IngestTextResponse(doc.Id, entities.Count);
    }
}