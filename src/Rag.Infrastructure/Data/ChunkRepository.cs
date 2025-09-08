using Microsoft.EntityFrameworkCore;
using Rag.Core.Domain.Entities;
using Rag.Core.Interfaces.Repositories;

namespace Rag.Infrastructure.Data;

public class ChunkRepository(AppDbContext db) : IChunkRepository
{
    public async Task InsertManyAsync(IEnumerable<Chunk> chunks)
    {
        await db.Chunks.AddRangeAsync(chunks);
        await db.SaveChangesAsync();
    }

    public async Task<IEnumerable<(Chunk Chunk, string? DocTitle, string? DocSource)>> QueryKnnAsync(float[] queryEmbedding, int k)
    {
        var sql = @"
            SELECT c.id, c.document_id, c.chunk_index, c.content, c.embedding, c.metadata_json, c.umap_x, c.umap_y,
                   d.title, d.source
            FROM chunks c
            JOIN documents d ON d.id = c.document_id
            ORDER BY c.embedding <=> @p0
            LIMIT @p1;";

        var results = await db.Set<KnnRow>()
            .FromSqlRaw(sql, queryEmbedding, k)
            .AsNoTracking()
            .ToListAsync();

        return results.Select(r => (
            new Chunk
            {
                Id = r.id,
                DocumentId = r.document_id,
                ChunkIndex = r.chunk_index,
                Content = r.content,
                Embedding = r.embedding,
                MetadataJson = r.metadata_json,
                UmapX = r.umap_x,
                UmapY = r.umap_y
            },
            r.title, r.source
        ));
    }

    public async Task UpdateProjectionAsync(IEnumerable<(Guid ChunkId, double X, double Y)> points)
    {
        var ids = points.Select(p => p.ChunkId).ToHashSet();
        var toUpdate = await db.Chunks.Where(c => ids.Contains(c.Id)).ToListAsync();
        var lookup = points.ToDictionary(p => p.ChunkId, p => (p.X, p.Y));
        foreach (var c in toUpdate)
        {
            var (x, y) = lookup[c.Id];
            c.UmapX = x;
            c.UmapY = y;
        }
        await db.SaveChangesAsync();
    }
}