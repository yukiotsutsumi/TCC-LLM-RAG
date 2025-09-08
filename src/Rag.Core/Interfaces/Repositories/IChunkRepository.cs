using Rag.Core.Domain.Entities;

namespace Rag.Core.Interfaces.Repositories
{
    public interface IChunkRepository
    {
        Task InsertManyAsync(IEnumerable<Chunk> chunks);
        Task<IEnumerable<(Chunk Chunk, string? DocTitle, string? DocSource)>> QueryKnnAsync(float[] queryEmbedding, int k);
        Task UpdateProjectionAsync(IEnumerable<(Guid ChunkId, double X, double Y)> points);
    }
}
