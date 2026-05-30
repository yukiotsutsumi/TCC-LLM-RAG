using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rag.Core.Domain.Entities;
using Rag.Core.Interfaces.Repositories;
using Rag.Infrastructure.Data;

namespace Rag.Infrastructure.Data.Repositories;

public class DocumentRepository(AppDbContext db, ILogger<DocumentRepository> logger) : IDocumentRepository
{
    private readonly ILogger<DocumentRepository> _logger = logger;
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

        var audit = new Rag.Core.Domain.Entities.DocumentAuditLog
        {
            DocumentId = doc.Id,
            Action = Rag.Core.Domain.Enums.DocumentAction.Delete,
            Details = $"Deleting document '{doc.Title}' with {doc.Chunks.Count} chunks.",
            PerformedByUserId = null,
            PerformedAt = DateTime.UtcNow
        };

        await db.DocumentAuditLogs.AddAsync(audit, ct);

        db.Documents.Remove(doc);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> GetTotalChunksAsync(CancellationToken ct = default) =>
        await db.Chunks.CountAsync(ct);

    public async Task AddDocumentAuditAsync(Rag.Core.Domain.Entities.DocumentAuditLog log, CancellationToken ct = default)
    {
        await db.DocumentAuditLogs.AddAsync(log, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> UpdateAccessLevelAsync(Guid id, Rag.Core.Domain.Enums.DocumentAccessLevel level, Guid? changedBy = null, CancellationToken ct = default)
    {
        var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null) return false;

        var old = doc.AccessLevel;
        doc.AccessLevel = level;
        db.Documents.Update(doc);

        var log = new Rag.Core.Domain.Entities.AccessLevelChangeLog
        {
            DocumentId = doc.Id,
            OldLevel = old,
            NewLevel = level,
            ChangedByUserId = changedBy,
            ChangedAt = DateTime.UtcNow
        };

        await db.AccessLevelChangeLogs.AddAsync(log, ct);

        _logger.LogInformation("Document {DocumentId} access level changed from {Old} to {New} by user {User}", doc.Id, old, level, changedBy);

        await db.SaveChangesAsync(ct);
        return true;
    }
}
