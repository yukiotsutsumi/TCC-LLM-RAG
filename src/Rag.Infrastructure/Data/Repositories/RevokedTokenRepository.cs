using Microsoft.EntityFrameworkCore;
using Rag.Core.Domain.Models;
using Rag.Core.Interfaces.Repositories;

namespace Rag.Infrastructure.Data.Repositories
{
    public class RevokedTokenRepository(AppDbContext db) : IRevokedTokenRepository
    {
        public async Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default)
            => await db.RevokedTokens.AnyAsync(x => x.Jti == jti, ct);

        public async Task AddAsync(RevokedToken token, CancellationToken ct = default)
            => await db.RevokedTokens.AddAsync(token, ct);

        public async Task PurgeExpiredAsync(CancellationToken ct = default)
        {
            var expired = await db.RevokedTokens
                .Where(x => x.ExpiresAt < DateTime.UtcNow)
                .ToListAsync(ct);

            db.RevokedTokens.RemoveRange(expired);
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
            => await db.SaveChangesAsync(ct);
    }
}