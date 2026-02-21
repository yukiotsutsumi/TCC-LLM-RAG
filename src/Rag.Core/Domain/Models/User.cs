using Microsoft.AspNetCore.Identity;

namespace Rag.Core.Domain.Models
{
    public class User : IdentityUser<Guid>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? FullName { get; set; }
    }
}
