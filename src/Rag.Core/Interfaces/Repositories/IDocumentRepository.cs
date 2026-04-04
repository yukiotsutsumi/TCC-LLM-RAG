using Rag.Core.Domain.Entities;

namespace Rag.Core.Interfaces.Repositories;

public interface IDocumentRepository
{
    Task InsertAsync(Document d);
    Task<Document?> GetAsync(Guid id);

    // ── novos ──────────────────────────────────────────────────
    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> GetTotalChunksAsync(CancellationToken ct = default);
}
