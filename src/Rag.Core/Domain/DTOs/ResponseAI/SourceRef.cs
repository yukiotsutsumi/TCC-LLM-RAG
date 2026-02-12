namespace Rag.Core.Domain.DTOs.ResponseIA
{
    public class SourceRef 
    { 
        public Guid ChunkId { get; set; } 
        public string Title { get; set; } = ""; 
        public string Source { get; set; } = ""; 
        public string Snippet { get; set; } = ""; 
    }
}
