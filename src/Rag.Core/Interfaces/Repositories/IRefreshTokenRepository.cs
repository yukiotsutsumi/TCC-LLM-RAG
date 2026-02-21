using Rag.Core.Domain.Models;

namespace Rag.Core.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
        Task AddAsync(RefreshToken token, CancellationToken ct = default);
        Task RevokeAllByFamilyAsync(Guid familyId, string reason, CancellationToken ct = default);
        Task RevokeAllByUserAsync(Guid userId, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}