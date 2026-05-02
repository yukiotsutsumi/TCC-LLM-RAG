using Rag.Core.Domain.Enums;

namespace Rag.Core.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string? Source { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DocumentAccessLevel AccessLevel { get; set; } = DocumentAccessLevel.User;

        public ICollection<Chunk> Chunks { get; set; } = [];
    }
}
