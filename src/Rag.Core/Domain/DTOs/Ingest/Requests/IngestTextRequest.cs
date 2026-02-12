namespace Rag.Core.Domain.DTOs.Ingest.Requests
{
    public record IngestTextRequest(string Title, string Source, string Text, string? Model = null);
}
