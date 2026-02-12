using Rag.Core.Domain.DTOs.Ingest.Requests;
using Rag.Core.Domain.DTOs.Ingest.Responses;

namespace Rag.Core.Interfaces.Services;

public interface IIngestionService
{
    Task<IngestTextResponse> IngestTextAsync(IngestTextRequest request, CancellationToken ct = default);
}