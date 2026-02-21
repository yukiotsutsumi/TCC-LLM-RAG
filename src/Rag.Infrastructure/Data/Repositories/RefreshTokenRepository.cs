using Microsoft.EntityFrameworkCore;
using Rag.Core.Domain.Models;
using Rag.Core.Interfaces.Repositories;

namespace Rag.Infrastructure.Data.Repositories
{
    public class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
    {
        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
            => await db.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == token, ct);

        public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
            => await db.RefreshTokens.AddAsync(token, ct);

        public async Task RevokeAllByFamilyAsync(Guid familyId, string reason, CancellationToken ct = default)
        {
            var tokens = await db.RefreshTokens
                .Where(x => x.FamilyId == familyId && x.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var t in tokens)
            {
                t.RevokedAt = DateTime.UtcNow;
                t.ReplacedByToken = reason;
            }
        }

        public async Task RevokeAllByUserAsync(Guid userId, CancellationToken ct = default)
        {
            var tokens = await db.RefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var t in tokens)
                t.RevokedAt = DateTime.UtcNow;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
            => await db.SaveChangesAsync(ct);
    }
}