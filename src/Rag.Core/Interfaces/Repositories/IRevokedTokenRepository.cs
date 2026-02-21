using Rag.Core.Domain.Models;

namespace Rag.Core.Interfaces.Repositories
{
    public interface IRevokedTokenRepository
    {
        Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default);
        Task AddAsync(RevokedToken token, CancellationToken ct = default);
        Task PurgeExpiredAsync(CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}