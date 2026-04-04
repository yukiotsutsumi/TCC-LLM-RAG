using Microsoft.EntityFrameworkCore;
using Rag.Core.Domain.Entities;
using Rag.Core.Interfaces.Repositories;
using Rag.Infrastructure.Data;

namespace Rag.Infrastructure.Data.Repositories;

public class DocumentRepository(AppDbContext db) : IDocumentRepository
{
    public async Task InsertAsync(Document d)
    {
        await db.Documents.AddAsync(d);
        await db.SaveChangesAsync();
    }

    public async Task<Document?> GetAsync(Guid id) =>
        await db.Documents
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default) =>
        await db.Documents
            .Include(d => d.Chunks)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await db.Documents
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (doc is null) return false;

        db.Documents.Remove(doc); // cascade deleta os chunks
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> GetTotalChunksAsync(CancellationToken ct = default) =>
        await db.Chunks.CountAsync(ct);
}
