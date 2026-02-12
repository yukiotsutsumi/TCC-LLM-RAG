namespace Rag.Core.Domain.DTOs.Ingest.Responses
{
    public record IngestTextResponse(Guid DocumentId, int ChunksSaved);
}
