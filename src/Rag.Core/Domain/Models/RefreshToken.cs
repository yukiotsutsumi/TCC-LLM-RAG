namespace Rag.Core.Domain.Models;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Token { get; set; } = default!;
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public Guid FamilyId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}