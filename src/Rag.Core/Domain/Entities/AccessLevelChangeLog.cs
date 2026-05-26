using Rag.Core.Domain.Enums;

namespace Rag.Core.Domain.Entities;

public class AccessLevelChangeLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public DocumentAccessLevel OldLevel { get; set; }
    public DocumentAccessLevel NewLevel { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
