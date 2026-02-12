using Microsoft.AspNetCore.Identity;

namespace Rag.Core.Domain.Models
{
    public class Role : IdentityRole<Guid>
    {
        public string? Description { get; set; }
    }
}
