namespace Rag.Core.Domain.DTOs
{
    public record IngestTextResponse(Guid DocumentId, int ChunksSaved);
}
