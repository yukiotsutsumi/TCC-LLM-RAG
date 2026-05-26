using Rag.Core.Domain.Enums;

namespace Rag.Core.Domain.Entities;

public class DocumentAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public DocumentAction Action { get; set; }
    public string? Details { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}
