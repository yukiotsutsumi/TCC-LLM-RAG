using Rag.Core.Domain.DTOs;
using Rag.Core.Domain.Entities;
using Rag.Core.Interfaces;
using Rag.Core.Interfaces.Repositories;
using Rag.Core.Interfaces.Services;

namespace Rag.Infrastructure.Llm;

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

        // 1) Documento
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Source = request.Source,
            CreatedAt = DateTime.UtcNow
        };
        await docs.InsertAsync(doc);

        // 2) Split em chunks (defaults do IChunker)
        var split = chunker.Split(request.Text).ToList();
        if (split.Count == 0)
            return new IngestTextResponse(doc.Id, 0);

        // 3) Gerar embeddings (um por chunk, conforme IOllamaClient atual)
        var model = string.IsNullOrWhiteSpace(request.Model) ? "mxbai-embed-large" : request.Model!;
        var entities = new List<Chunk>(split.Count);

        foreach (var part in split)
        {
            // IOllamaClient.EmbedAsync(model, text)
            var embedding = await ollama.EmbedAsync(model, part.Content);

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

        // 4) Persistir todos os chunks
        await chunkRepo.InsertManyAsync(entities);

        return new IngestTextResponse(doc.Id, entities.Count);
    }
}