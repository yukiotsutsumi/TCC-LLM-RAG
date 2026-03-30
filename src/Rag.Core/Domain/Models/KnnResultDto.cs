namespace Rag.Core.Domain.Models;

public record KnnResultDto(
    Rag.Core.Domain.Entities.Chunk Chunk,
    string? DocumentTitle,
    string? DocumentSource,
    double Score  // ← distância cosine (0 = idêntico, 2 = oposto)
);
