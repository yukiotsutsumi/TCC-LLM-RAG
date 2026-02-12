namespace Rag.Core.Domain.DTOs.Auth.Request
{
    public record RegisterRequest(string Username, string Email, string Password, string FullName);
}
