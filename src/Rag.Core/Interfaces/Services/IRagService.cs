using Rag.Core.Domain.DTOs;

namespace Rag.Core.Interfaces.Services
{
    public interface IRagService
    {
        Task<AskResponse> AskAsync(AskRequest request);
    }
}
