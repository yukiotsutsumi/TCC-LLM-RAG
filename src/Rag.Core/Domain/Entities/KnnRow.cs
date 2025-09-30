using Pgvector;

namespace Rag.Core.Domain.Entities
{
    public class KnnRow
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = default!;
        public Vector? Embedding { get; set; }
        public string? MetadataJson { get; set; }
        public double? UmapX { get; set; }
        public double? UmapY { get; set; }
        public string? Title { get; set; }
        public string? Source { get; set; }
    }
}
