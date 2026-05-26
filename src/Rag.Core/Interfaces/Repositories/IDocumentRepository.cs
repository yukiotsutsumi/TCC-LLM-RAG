using Rag.Core.Domain.Entities;
using Rag.Core.Domain.Enums;

namespace Rag.Core.Interfaces.Repositories;

public interface IDocumentRepository
{
    Task InsertAsync(Document d);
    Task<Document?> GetAsync(Guid id);

    // ── novos ──────────────────────────────────────────────────
    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> GetTotalChunksAsync(CancellationToken ct = default);
    Task<bool> UpdateAccessLevelAsync(Guid id, DocumentAccessLevel level, Guid? changedBy = null, CancellationToken ct = default);
    Task AddDocumentAuditAsync(DocumentAuditLog log, CancellationToken ct = default);
}
