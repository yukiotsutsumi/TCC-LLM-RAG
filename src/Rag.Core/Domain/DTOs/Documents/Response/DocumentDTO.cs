namespace Rag.Core.Domain.DTOs.Documents.Response
{
    using Rag.Core.Domain.Enums;

    public record DocumentDto(
        Guid Id,
        string Title,
        string? Source,
        DateTime CreatedAt,
        int ChunkCount,
        string Status,
        DocumentAccessLevel AccessLevel
    );
};