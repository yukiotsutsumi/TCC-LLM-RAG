namespace Rag.Core.Domain.DTOs.Auth.Response
{
    public record AuthResponse(bool Success, string Message, AuthSuccessResponse? Data = null);
}
