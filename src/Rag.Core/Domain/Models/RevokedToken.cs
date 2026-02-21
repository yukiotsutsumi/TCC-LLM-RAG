namespace Rag.Core.Domain.Models;

public class RevokedToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Jti { get; set; } = default!;
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
}