using Rag.Core.Domain.DTOs;
using System.Runtime.CompilerServices;

namespace Rag.Core.Interfaces.Services
{
    public interface IRagService
    {
        Task<AskResponse> AskAsync(AskRequest request);
        IAsyncEnumerable<StreamPart> AskStreamAsync(AskRequest request, CancellationToken ct = default);
    }
}
