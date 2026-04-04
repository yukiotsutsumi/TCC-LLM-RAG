namespace Rag.Core.Domain.DTOs.Documents.Response
{
    public record DocumentStatsDto(
        int TotalDocuments,
        int TotalChunks
    );
}