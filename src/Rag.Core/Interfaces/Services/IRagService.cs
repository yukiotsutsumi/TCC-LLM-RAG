using Rag.Core.Domain.DTOs.Ask.Requests;
using Rag.Core.Domain.DTOs.ResponseAI;
using Rag.Core.Domain.Enums;

namespace Rag.Core.Interfaces.Services
{
    public interface IRagService
    {
        IAsyncEnumerable<StreamPart> AskStreamAsync(
        AskRequest request,
        DocumentAccessLevel accessLevel,
        CancellationToken ct = default);
    }
}
