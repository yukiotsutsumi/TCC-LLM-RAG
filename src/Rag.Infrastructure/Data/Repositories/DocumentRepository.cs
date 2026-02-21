using Microsoft.EntityFrameworkCore;
using Rag.Core.Domain.Entities;
using Rag.Core.Interfaces.Repositories;

namespace Rag.Infrastructure.Data.Repositories;

public class DocumentRepository(AppDbContext db) : IDocumentRepository
{
    public async Task InsertAsync(Document d)
    {
        db.Documents.Add(d);
        await db.SaveChangesAsync();
    }

    public async Task<Document?> GetAsync(Guid id)
    {
        return await db.Documents
            .Include(x => x.Chunks)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}