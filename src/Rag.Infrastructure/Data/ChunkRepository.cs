using Microsoft.EntityFrameworkCore;
using Pgvector;
using Rag.Core.Domain.Entities;
using Rag.Core.Interfaces.Repositories;
using Rag.Core.Domain.Models;

namespace Rag.Infrastructure.Data;

public class ChunkRepository(AppDbContext db) : IChunkRepository
{
    private const int EmbeddingDim = 1024; // ajuste se sua dimensão mudar
    private const int MaxK = 500;          // proteção para consultas muito grandes

    public async Task InsertManyAsync(IEnumerable<Chunk> chunks, CancellationToken ct = default)
    {
        foreach (var c in chunks)
        {
            ValidateEmbedding(c.Embedding);
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.Chunks.AddRangeAsync(chunks, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
    }

    public async Task UpdateProjectionAsync(IEnumerable<(Guid ChunkId, double X, double Y)> points, CancellationToken ct = default)
    {
        var ids = points.Select(p => p.ChunkId).ToHashSet();
        var lookup = points.ToDictionary(p => p.ChunkId, p => (p.X, p.Y));

        var toUpdate = await db.Chunks
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(ct);

        var originalDetect = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            foreach (var c in toUpdate)
            {
                var (x, y) = lookup[c.Id];
                c.UmapX = x;
                c.UmapY = y;
            }
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = originalDetect;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<KnnResultDto>> QueryKnnAsync(
        Vector queryEmbedding,
        int k,
        KnnMetric metric = KnnMetric.Cosine,
        CancellationToken ct = default)
    {
        ValidateEmbedding(queryEmbedding);

        if (k <= 0) k = 1;
        if (k > MaxK) k = MaxK;

        var op = MetricToSqlOperator(metric);

        var sql = $@"
        SELECT 
            c.""Id""           AS ""Id"",
            c.""DocumentId""   AS ""DocumentId"",
            c.""ChunkIndex""   AS ""ChunkIndex"",
            c.""Content""      AS ""Content"",
            c.""Embedding""    AS ""Embedding"",
            c.""MetadataJson"" AS ""MetadataJson"",
            c.""UmapX""        AS ""UmapX"",
            c.""UmapY""        AS ""UmapY"",
            d.""Title""        AS ""Title"",
            d.""Source""       AS ""Source""
        FROM ""Chunks"" c
        JOIN ""Documents"" d ON d.""Id"" = c.""DocumentId""
        ORDER BY c.""Embedding"" {op} @p0
        LIMIT @p1;";
        
        var results = await db.Set<KnnRow>()
            .FromSqlRaw(sql, queryEmbedding, k)
            .AsNoTracking()
            .ToListAsync(ct);

        // Projeta para DTO
        var list = results.Select(r => new KnnResultDto(
            new Chunk
            {
                Id = r.Id,
                DocumentId = r.DocumentId,
                ChunkIndex = r.ChunkIndex,
                Content = r.Content,
                Embedding = r.Embedding,
                MetadataJson = r.MetadataJson,
                UmapX = r.UmapX,
                UmapY = r.UmapY
            },
            r.Title,
            r.Source
        )).ToList();

        return list;
    }

    // Helpers

    private static string MetricToSqlOperator(KnnMetric metric) => metric switch
    {
        KnnMetric.Cosine => "<=>",       // cosine distance
        KnnMetric.L2 => "<->",           // euclidean (L2)
        KnnMetric.InnerProduct => "<#>", // inner product
        _ => "<=>"
    };
    private static void ValidateEmbedding(Vector? vec)  
    {  
        if (vec is null)  
            throw new ArgumentException("Embedding não pode ser nulo.");  
    
        var len = vec.ToArray().Length;  
        if (len != EmbeddingDim)  
            throw new ArgumentException($"Embedding com dimensão inválida: esperado {EmbeddingDim}, recebido {len}.");  
    }  
    
    private static void ValidateEmbedding(Vector vec, int expectedDim)  
    {  
        var len = vec.ToArray().Length;  
        if (len != expectedDim)  
            throw new ArgumentException($"Embedding com dimensão inválida: esperado {expectedDim}, recebido {len}.");  
    }
}