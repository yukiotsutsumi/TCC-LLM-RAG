using System.ComponentModel.DataAnnotations;

namespace Rag.Core.Domain.DTOs.Auth.Request
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}