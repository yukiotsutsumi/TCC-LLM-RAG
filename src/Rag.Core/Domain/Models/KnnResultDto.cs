namespace Rag.Core.Domain.Models;

public record KnnResultDto(  
    Rag.Core.Domain.Entities.Chunk Chunk,  
    string? DocumentTitle,  
    string? DocumentSource  
);