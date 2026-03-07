using Rag.Core.Domain.DTOs.Ask.Requests;
using Rag.Core.Domain.DTOs.Ask.Responses;
using Rag.Core.Domain.DTOs.ResponseAI;

namespace Rag.Core.Interfaces.Services
{
    public interface IRagService
    {
        Task<AskResponse> AskAsync(AskRequest request);
        IAsyncEnumerable<StreamPart> AskStreamAsync(AskRequest request, CancellationToken ct = default);
    }
}
