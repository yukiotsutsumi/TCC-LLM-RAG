namespace Rag.Core.Domain.DTOs
{
    public record IngestTextRequest(string Title, string Source, string Text, string? Model = null);
}
