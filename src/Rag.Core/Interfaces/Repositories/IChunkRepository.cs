using Pgvector;
using Rag.Core.Domain.Entities;
using Rag.Core.Domain.Models;

namespace Rag.Core.Interfaces.Repositories
{
    public interface IChunkRepository
    {
        Task InsertManyAsync(IEnumerable<Chunk> chunks, CancellationToken ct = default);
        Task UpdateProjectionAsync(IEnumerable<(Guid ChunkId, double X, double Y)> points, CancellationToken ct = default);
        Task<IReadOnlyList<KnnResultDto>> QueryKnnAsync(
            Vector queryEmbedding,
            int k,
            KnnMetric metric = KnnMetric.Cosine,
            CancellationToken ct = default);
    }
}
