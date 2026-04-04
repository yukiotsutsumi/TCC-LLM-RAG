namespace Rag.Core.Domain.DTOs.Documents.Response
{
    public record DocumentDto(
        Guid Id,
        string Title,
        string? Source,
        DateTime CreatedAt,
        int ChunkCount,
        string Status
    );
};