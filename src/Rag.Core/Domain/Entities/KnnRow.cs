namespace Rag.Core.Domain.Entities
{
    public class KnnRow
    {
        public Guid Id { get; set; }
        public Guid Document_id { get; set; }
        public int Chunk_index { get; set; }
        public string Content { get; set; } = "";
        public float[]? Embedding { get; set; }
        public string? Metadata_json { get; set; }
        public double? Umap_x { get; set; }
        public double? Umap_y { get; set; }
        public string? Title { get; set; }
        public string? Source { get; set; }
    }
}
