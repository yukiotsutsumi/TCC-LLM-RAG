using Rag.Core.Domain.DTOs.Auth.Request;
using Rag.Core.Domain.DTOs.Auth.Response;

namespace Rag.Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }

}
