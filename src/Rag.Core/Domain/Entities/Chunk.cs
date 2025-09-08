namespace Rag.Core.Domain.Entities
{
    public class Chunk
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = "";
        public float[]? Embedding { get; set; }
        public string? MetadataJson { get; set; }
        public double? UmapX { get; set; }
        public double? UmapY { get; set; }

        public Document? Document { get; set; }
    }
}
