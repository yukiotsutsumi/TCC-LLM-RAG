using Rag.Core.Domain.DTOs;

namespace Rag.Core.Interfaces.Services;

public interface IIngestionService
{
    Task<IngestTextResponse> IngestTextAsync(IngestTextRequest request, CancellationToken ct = default);
}